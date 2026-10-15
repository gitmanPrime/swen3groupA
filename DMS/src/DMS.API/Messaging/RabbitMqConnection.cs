using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace DMS.API.Messaging;

// Keeps the broker connection open for the lifetime of the REST server.
public sealed class RabbitMqConnection(IOptions<RabbitMqOptions> options, ILogger<RabbitMqConnection> logger) : IHostedService, IAsyncDisposable
{
    private IConnection? _connection;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var factory = new ConnectionFactory
        {
            HostName = settings.HostName,
            Port = settings.Port,
            UserName = settings.UserName,
            Password = settings.Password,
            ClientProvidedName = "dms-api"
        };

        try
        {
            _connection = await factory.CreateConnectionAsync(cancellationToken);
            await using var channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
            await channel.QueueDeclareAsync(queue: settings.QueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);

            logger.LogInformation("RabbitMQ connected; queue {QueueName} is ready", settings.QueueName);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "RabbitMQ initialization failed for {HostName}:{Port}", settings.HostName, settings.Port);
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true })
            await _connection.CloseAsync(cancellationToken: cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
            await _connection.DisposeAsync();
    }
}
