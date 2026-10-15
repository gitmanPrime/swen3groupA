using System.Text.Json;
using DMS.BLL.Dtos;
using DMS.BLL.Interfaces.Services;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace DMS.API.Messaging;

public sealed class OcrQueuePublisher(IConnection connection, IOptions<RabbitMqOptions> options, ILogger<OcrQueuePublisher> logger) : IOcrQueuePublisher, IAsyncDisposable
{
    // RabbitMQ channels must not be used by concurrent publishers.
    private readonly SemaphoreSlim _publishLock = new(1, 1);
    private IChannel? _channel;

    public async Task PublishAsync(OcrRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.DocumentId == Guid.Empty)
            throw new ArgumentException("Document ID is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.FileName))
            throw new ArgumentException("File name is required.", nameof(request));

        await _publishLock.WaitAsync(cancellationToken);
        try
        {
            _channel ??= await connection.CreateChannelAsync(new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true), cancellationToken);

            await _channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: options.Value.QueueName,
                mandatory: true,
                basicProperties: new BasicProperties
                {
                    ContentType = "application/json",
                    Persistent = true
                },
                body: JsonSerializer.SerializeToUtf8Bytes(request),
                cancellationToken: cancellationToken);

            logger.LogInformation("Published OCR request for document {DocumentId}", request.DocumentId);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to publish OCR request for document {DocumentId}", request.DocumentId);
            throw;
        }
        finally
        {
            _publishLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
            await _channel.DisposeAsync();
        _publishLock.Dispose();
    }
}
