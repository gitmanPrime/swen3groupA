using System.Net;
using System.Text.Json;
using DMS.BLL.Dtos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DMS.IndexingWorker;

internal sealed class IndexingWorker(
    IConfiguration configuration,
    ElasticsearchDocumentWriter writer,
    ILogger<IndexingWorker> logger) : BackgroundService
{
    private const int MaxIndexAttempts = 3;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = configuration.GetSection("RabbitMQ");
        var queue = settings["ResultQueueName"] ?? "ocr.results";
        var failedQueue = $"{queue}.failed";

        var factory = new ConnectionFactory
        {
            HostName = settings["HostName"] ?? "localhost",
            Port = settings.GetValue("Port", 5672),
            UserName = settings["UserName"] ?? "dms",
            Password = settings["Password"] ?? "dms",
            ClientProvidedName = "dms-indexing-worker"
        };

        await using var connection = await factory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(
            new CreateChannelOptions(
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: true),
            cancellationToken: stoppingToken);

        foreach (var queueName in new[] { queue, failedQueue })
        {
            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: stoppingToken);
        }

        await channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 1,
            global: false,
            cancellationToken: stoppingToken);

        var consumerFailure = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, delivery) =>
        {
            try
            {
                await ProcessAsync(channel, delivery, failedQueue, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Closing the channel returns unacknowledged deliveries to RabbitMQ.
            }
            catch (Exception exception)
            {
                consumerFailure.TrySetException(exception);
            }
        };

        await channel.BasicConsumeAsync(
            queue: queue,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        logger.LogInformation("Indexing worker is listening to {QueueName}", queue);
        await consumerFailure.Task.WaitAsync(stoppingToken);
    }

    private async Task ProcessAsync(
        IChannel channel,
        BasicDeliverEventArgs delivery,
        string failedQueue,
        CancellationToken cancellationToken)
    {
        OcrResult? result = null;

        try
        {
            result = JsonSerializer.Deserialize<OcrResult>(delivery.Body.Span);
            if (result is null || result.DocumentId == Guid.Empty || result.Text is null)
            {
                throw new JsonException("OCR result requires a document ID and text.");
            }

            await IndexWithRetryAsync(result, cancellationToken);
            logger.LogInformation("Indexed OCR result for document {DocumentId}", result.DocumentId);
        }
        catch (Exception exception) when (
            !cancellationToken.IsCancellationRequested &&
            exception is (JsonException or HttpRequestException or OperationCanceledException))
        {
            logger.LogError(exception,
                "Could not index document {DocumentId} from delivery {DeliveryTag}. Sending it to {FailedQueue}",
                result?.DocumentId, delivery.DeliveryTag, failedQueue);

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: failedQueue,
                mandatory: true,
                basicProperties: new BasicProperties
                {
                    ContentType = "application/json",
                    Persistent = true
                },
                body: delivery.Body,
                cancellationToken: cancellationToken);
        }

        await channel.BasicAckAsync(
            delivery.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
    }

    private async Task IndexWithRetryAsync(OcrResult result, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await writer.IndexAsync(result, cancellationToken);
                return;
            }
            catch (Exception exception) when (
                attempt < MaxIndexAttempts &&
                !cancellationToken.IsCancellationRequested &&
                IsTransient(exception))
            {
                logger.LogWarning(exception,
                    "Indexing attempt {Attempt} failed for document {DocumentId}. Retrying",
                    attempt, result.DocumentId);
                await Task.Delay(TimeSpan.FromSeconds(attempt), cancellationToken);
            }
        }
    }

    private static bool IsTransient(Exception exception) => exception switch
    {
        OperationCanceledException => true,
        HttpRequestException httpException =>
            httpException.StatusCode is null or HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests ||
            (int)httpException.StatusCode.GetValueOrDefault() >= 500,
        _ => false
    };
}