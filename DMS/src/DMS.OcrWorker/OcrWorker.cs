using DMS.BLL.Dtos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text.Json;
using System.Threading.Channels;

namespace DMS.OcrWorker
{
    public class OcrWorker(IOptions<RabbitMqOptions> options, ILogger<OcrWorker> logger) : BackgroundService
    {
        private readonly RabbitMqOptions _options = options.Value;
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("OCR Worker is starting...");
            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
                ClientProvidedName = "dms-ocr-worker"
            };

            // waiting for a connection, if RabbitMQ is still booting up at startup
            IConnection? connection = null;
            IChannel? channel = null;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    connection = await factory.CreateConnectionAsync(stoppingToken);
                    channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

                    await channel.QueueDeclareAsync(
                        queue: _options.QueueName,
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null,
                        cancellationToken: stoppingToken);

                    logger.LogInformation("Connected to RabbitMQ. Listening to queue '{QueueName}'...", _options.QueueName);
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to connect to RabbitMQ at {HostName}:{Port}. Retrying in 5 seconds...", _options.HostName, _options.Port);
                    await Task.Delay(5000, stoppingToken);
                }
            }

            if (channel is null)
            {
                return;
            }

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (sender, ea) =>
            {
                OcrRequest? ocrRequest = null;
                try
                {
                    var body = ea.Body.Span;
                    ocrRequest = JsonSerializer.Deserialize<OcrRequest>(body);

                    if (ocrRequest is null)
                        throw new JsonException("OCR request must not be null.");

                    logger.LogInformation(
                        "Received OCR request for document {DocumentId} from queue {QueueName} (delivery {DeliveryTag})",
                        ocrRequest.DocumentId, _options.QueueName, ea.DeliveryTag);

                    await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Failed to process OCR message for document {DocumentId} from queue {QueueName} (delivery {DeliveryTag})",
                        ocrRequest?.DocumentId, _options.QueueName, ea.DeliveryTag);
                    await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                }
            };

            await channel.BasicConsumeAsync(
                queue: _options.QueueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            // block as long as the service is still running
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}
