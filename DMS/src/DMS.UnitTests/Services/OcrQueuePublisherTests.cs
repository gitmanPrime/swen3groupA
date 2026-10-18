using System.Text.Json;
using DMS.API.Messaging;
using DMS.BLL.Dtos;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using RabbitMQ.Client;
using DMS.BLL.Exceptions;

namespace DMS.UnitTests.Services;

public class OcrQueuePublisherTests
{
    private readonly Mock<IConnection> _connection = new();
    private readonly Mock<IChannel> _channel = new();

    private OcrQueuePublisher CreatePublisher()
    {
        _connection.Setup(connection => connection.CreateChannelAsync(
                It.IsAny<CreateChannelOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_channel.Object);

        return new OcrQueuePublisher(_connection.Object,
            Options.Create(new RabbitMqOptions { QueueName = "ocr.documents" }),
            NullLogger<OcrQueuePublisher>.Instance);
    }

    [Fact]
    public async Task PublishAsync_SendsPersistentJsonToOcrQueue()
    {
        await using var publisher = CreatePublisher();
        var request = new OcrRequest(Guid.NewGuid(), "example.pdf");
        byte[]? body = null;
        _channel.Setup(channel => channel.BasicPublishAsync(
                It.IsAny<string>(), It.IsAny<string>(), true,
                It.IsAny<BasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, bool, BasicProperties, ReadOnlyMemory<byte>, CancellationToken>(
                (exchange, routingKey, mandatory, properties, payload, token) => body = payload.ToArray())
            .Returns(ValueTask.CompletedTask);

        await publisher.PublishAsync(request);

        Assert.Equal(request, JsonSerializer.Deserialize<OcrRequest>(body!));
        _channel.Verify(channel => channel.BasicPublishAsync(
            It.Is<string>(exchange => exchange.ToString() == ""),
            It.Is<string>(key => key.ToString() == "ocr.documents"), true,
            It.Is<BasicProperties>(properties => properties.Persistent && properties.ContentType == "application/json"),
            It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_BrokerFailure_PreservesOriginalCause()
    {
        await using var publisher = CreatePublisher();
        var brokerError = new IOException("Broker unavailable");

        _channel.Setup(channel => channel.BasicPublishAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                true,
                It.IsAny<BasicProperties>(),
                It.IsAny<ReadOnlyMemory<byte>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(brokerError);

        var exception = await Assert.ThrowsAsync<MessagingException>(() =>
            publisher.PublishAsync(
                new OcrRequest(Guid.NewGuid(), "example.pdf")));

        Assert.Same(brokerError, exception.InnerException);
    }
}

