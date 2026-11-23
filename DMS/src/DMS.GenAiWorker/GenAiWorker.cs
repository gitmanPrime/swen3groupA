using DMS.API.Messaging;
using DMS.BLL.Dtos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text.Json;
using static DMS.BLL.Dtos.DocumentSummaryResultDto;

namespace DMS.GenAiWorker;

public class GenAiWorker(
    IOptions<RabbitMqOptions> rabbitMqOptions,
    ILogger<GenAiWorker> logger,
    IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
    private readonly RabbitMqOptions _rabbitMqOptions = rabbitMqOptions.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("GenAiWorker is starting.");

        if (string.IsNullOrWhiteSpace(_rabbitMqOptions.SummaryQueueName) ||
            string.Equals(_rabbitMqOptions.QueueName, _rabbitMqOptions.SummaryQueueName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "RabbitMQ requires a summary queue distinct from the input OCR result queue.");
        }

        var factory = new ConnectionFactory
        {
            HostName = _rabbitMqOptions.HostName,
            Port = _rabbitMqOptions.Port,
            UserName = _rabbitMqOptions.UserName,
            Password = _rabbitMqOptions.Password,
            ClientProvidedName = "dms-genai-worker"
        };

        IConnection? connection = null;
        IChannel? channel = null;

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    connection = await factory.CreateConnectionAsync(stoppingToken);

                    channel = await connection.CreateChannelAsync(
                        new CreateChannelOptions(
                            publisherConfirmationsEnabled: true,
                            publisherConfirmationTrackingEnabled: true),
                        cancellationToken: stoppingToken);

                    await channel.QueueDeclareAsync(
                        queue: _rabbitMqOptions.QueueName,
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null,
                        cancellationToken: stoppingToken);

                    await channel.QueueDeclareAsync(
                        queue: _rabbitMqOptions.SummaryQueueName,
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null,
                        cancellationToken: stoppingToken);

                    await channel.BasicQosAsync(
                        prefetchSize: 0,
                        prefetchCount: 1,
                        global: false,
                        cancellationToken: stoppingToken);

                    logger.LogInformation(
                        "Connected to RabbitMQ. Listening to queue '{QueueName}'...",
                        _rabbitMqOptions.QueueName);

                    break;
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(
                        ex,
                        "Failed to connect to RabbitMQ at {HostName}:{Port}. Retrying in 5 seconds...",
                        _rabbitMqOptions.HostName,
                        _rabbitMqOptions.Port);

                    if (channel is not null)
                    {
                        await channel.DisposeAsync();
                        channel = null;
                    }

                    if (connection is not null)
                    {
                        await connection.DisposeAsync();
                        connection = null;
                    }

                    await Task.Delay(5000, stoppingToken);
                }
            }

            if (channel is null)
            {
                return;
            }

            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.ReceivedAsync += async (_, ea) =>
            {
                OcrResult? ocrResult = null;

                try
                {
                    ocrResult = JsonSerializer.Deserialize<OcrResult>(ea.Body.Span);

                    if (ocrResult is null || ocrResult.DocumentId == Guid.Empty)
                    {
                        throw new JsonException("OCR result payload is null or contains an empty DocumentId.");
                    }

                    logger.LogInformation(
                        "Received OCR result for document {DocumentId} from queue '{QueueName}' (delivery {DeliveryTag})",
                        ocrResult.DocumentId,
                        _rabbitMqOptions.QueueName,
                        ea.DeliveryTag);

                    if (string.IsNullOrWhiteSpace(ocrResult.Text))
                    {
                        logger.LogWarning(
                            "Document {DocumentId} has no extracted text; skipping GenAI summarization.",
                            ocrResult.DocumentId);

                        await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                        return;
                    }

                    using var scope = serviceScopeFactory.CreateScope();
                    var summarizer = scope.ServiceProvider.GetRequiredService<IGenAiSummarizer>();

                    var summary = await summarizer.SummarizeAsync(ocrResult.Text, stoppingToken);

                    var summaryResult = new DocumentSummaryResult(ocrResult.DocumentId, summary);

                    await channel.BasicPublishAsync(
                        exchange: string.Empty,
                        routingKey: _rabbitMqOptions.SummaryQueueName,
                        mandatory: true,
                        basicProperties: new BasicProperties
                        {
                            ContentType = "application/json",
                            Persistent = true
                        },
                        body: JsonSerializer.SerializeToUtf8Bytes(summaryResult),
                        cancellationToken: stoppingToken);

                    await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);

                    logger.LogInformation(
                        "Published summary for document {DocumentId} to queue '{SummaryQueueName}'.",
                        ocrResult.DocumentId,
                        _rabbitMqOptions.SummaryQueueName);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    logger.LogWarning(
                        "GenAI processing cancelled for document {DocumentId}.",
                        ocrResult?.DocumentId);

                    try
                    {
                        await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Could not requeue cancelled GenAI message.");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "Failed to process GenAI summarization for document {DocumentId} (delivery {DeliveryTag}).",
                        ocrResult?.DocumentId,
                        ea.DeliveryTag);

                    try
                    {
                        await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                    }
                    catch (Exception nackException)
                    {
                        logger.LogWarning(nackException, "Failed to reject GenAI message.");
                    }
                }
            };

            await channel.BasicConsumeAsync(
                queue: _rabbitMqOptions.QueueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        finally
        {
            if (channel is not null)
            {
                await channel.DisposeAsync();
            }

            if (connection is not null)
            {
                await connection.DisposeAsync();
            }

            logger.LogInformation("GenAI Worker stopped.");
        }
    }
}