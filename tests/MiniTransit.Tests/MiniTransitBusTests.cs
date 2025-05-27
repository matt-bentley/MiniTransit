using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MiniTransit.Policies;
using MiniTransit.Serialization;
using MiniTransit.Settings;
using MiniTransit.Tests.Policies;
using Moq;

namespace MiniTransit.Tests;

public class MiniTransitBusTests
{
    private readonly MiniTransitBus _miniTransitBus;
    private readonly IOptions<MiniTransitSettings> _options;
    private readonly MockRetryPolicy _retryPolicy = new();

    public MiniTransitBusTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMessageSerializer, JsonMessageSerializer>();
        services.AddTransient<TestConsumer>();
        services.AddTransient<TestConsumer2>();
        services.AddSingleton<IRetryPolicy>(_retryPolicy);

        _options = Options.Create(new MiniTransitSettings
        {
            PublishTopic = "default-topic",
            SubscribeTopic = "default-subscribe-topic"
        });
        services.AddSingleton(_options);

        var serviceProvider = services.BuildServiceProvider();

        var messageBus = new InMemoryMessageBus(Mock.Of<ILogger<InMemoryMessageBus>>());

        _miniTransitBus = new MiniTransitBus(
            messageBus,
            serviceProvider,
            Mock.Of<ILogger<MiniTransitBus>>(),
            _options,
            new JsonMessageSerializer()
        );
    }

    [Fact]
    public async Task SubscribeAsync_ShouldHandlePublishedMessage()
    {
        // Arrange
        var message = new TestMessage(){ Content = "Test Subscription Message" };
        var receivedMessages = new List<string>();

        await _miniTransitBus.SubscribeAsync<TestMessage, TestConsumer>("default-topic");

        TestConsumer.OnMessageReceived = msg =>
        {
            receivedMessages.Add(msg.Message.Content);
            return Task.CompletedTask;
        };

        await _miniTransitBus.StartProcessingAsync();

        // Act
        await _miniTransitBus.PublishAsync(message);

        // Allow some time for the message to be processed
        await Task.Delay(100);

        // Assert
        Assert.Single(receivedMessages);
        Assert.Equal(message.Content, receivedMessages.First());
    }

    [Fact]
    public async Task PublishAsync_ShouldPublishToOverrideTopic()
    {
        // Arrange
        var message = new TestMessage { Content = "Override Topic Message" };
        var receivedMessages = new List<string>();

        await _miniTransitBus.SubscribeAsync<TestMessage, TestConsumer>("override-topic");

        TestConsumer.OnMessageReceived = msg =>
        {
            receivedMessages.Add(msg.Message.Content);
            return Task.CompletedTask;
        };

        await _miniTransitBus.StartProcessingAsync();

        // Act
        await _miniTransitBus.PublishAsync(message, "override-topic");

        // Allow some time for the message to be processed
        await Task.Delay(100);

        // Assert
        Assert.Single(receivedMessages);
        Assert.Equal(message.Content, receivedMessages.First());
    }

    [Fact]
    public async Task ScheduleAsync_ShouldThrowException_WhenDelayIsLessThanZero()
    {
        // Arrange
        var message = new TestMessage { Content = "Invalid Delay" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _miniTransitBus.ScheduleAsync(message, TimeSpan.FromSeconds(-1)));
    }

    [Fact]
    public async Task ScheduleAsync_ShouldPublishMessageAfterDelay()
    {
        // Arrange
        var message = new TestMessage { Content = "Scheduled Message" };
        var receivedMessages = new List<string>();

        await _miniTransitBus.SubscribeAsync<TestMessage, TestConsumer>("default-topic");

        TestConsumer.OnMessageReceived = msg =>
        {
            receivedMessages.Add(msg.Message.Content);
            return Task.CompletedTask;
        };

        await _miniTransitBus.StartProcessingAsync();

        // Act
        await _miniTransitBus.ScheduleAsync(message, TimeSpan.FromMilliseconds(500));

        await Task.Delay(300);
        Assert.Empty(receivedMessages);

        // Allow some time for the message to be processed
        await Task.Delay(500);

        // Assert
        Assert.Single(receivedMessages);
        Assert.Equal(message.Content, receivedMessages.First());
    }

    [Fact]
    public async Task StopProcessingAsync_ShouldStopMessageProcessing()
    {
        // Arrange
        var message = new TestMessage { Content = "Message After Stop" };
        var receivedMessages = new List<string>();

        await _miniTransitBus.SubscribeAsync<TestMessage, TestConsumer>("default-topic");

        TestConsumer.OnMessageReceived = msg =>
        {
            receivedMessages.Add(msg.Message.Content);
            return Task.CompletedTask;
        };

        await _miniTransitBus.StartProcessingAsync();
        await _miniTransitBus.StopProcessingAsync();

        // Act
        await _miniTransitBus.PublishAsync(message);

        // Allow some time for the message to be processed
        await Task.Delay(100);

        // Assert
        Assert.Empty(receivedMessages);
    }

    [Fact]
    public async Task HandleMessageAsync_ShouldRetryOnFailure()
    {
        // Arrange
        _retryPolicy.MaxRetryCount = 3;
        var message = new TestMessage { Content = "Retry Test Message" };
        var receivedMessages = new List<string>();
        var retryAttempts = 0;

        TestConsumer.OnMessageReceived = msg =>
        {
            retryAttempts++;
            if (retryAttempts < 3) // Simulate failure for the first two attempts
            {
                throw new InvalidOperationException("Simulated failure");
            }
            receivedMessages.Add(msg.Message.Content);
            return Task.CompletedTask;
        };

        await _miniTransitBus.SubscribeAsync<TestMessage, TestConsumer>("default-topic");
        await _miniTransitBus.StartProcessingAsync();

        // Act
        await _miniTransitBus.PublishAsync(message);

        // Allow time for retries
        await Task.Delay(200);

        // Assert
        Assert.Single(receivedMessages);
        Assert.Equal(message.Content, receivedMessages.First());
        Assert.Equal(3, retryAttempts); // 2 failures + 1 success
    }

    [Fact]
    public async Task HandleMessageAsync_ShouldDiscardMessageAfterMaxRetries()
    {
        // Arrange
        _retryPolicy.MaxRetryCount = 3;
        var message = new TestMessage { Content = "Max Retry Test Message" };
        var retryAttempts = 0;

        TestConsumer.OnMessageReceived = msg =>
        {
            retryAttempts++;
            throw new InvalidOperationException("Simulated failure");
        };

        await _miniTransitBus.SubscribeAsync<TestMessage, TestConsumer>("default-topic");
        await _miniTransitBus.StartProcessingAsync();

        // Act
        await _miniTransitBus.PublishAsync(message);

        // Allow time for retries
        await Task.Delay(200);

        // Assert
        Assert.Equal(_retryPolicy.MaxRetryCount, retryAttempts);
    }

    [Fact]
    public async Task HandleMessageAsync_ShouldIgnoreMessageFromDifferentConsumer()
    {
        // Arrange
        _retryPolicy.MaxRetryCount = 3;
        var message = new TestMessage { Content = "Different Consumer Test Message" };
        var retryAttempts = 0;
        var retryAttempts2 = 0;

        TestConsumer.OnMessageReceived = msg =>
        {
            retryAttempts++;
            throw new InvalidOperationException("Simulated failure");
        };
        TestConsumer2.OnMessageReceived = msg =>
        {
            retryAttempts2++;
            return Task.CompletedTask;
        };

        await _miniTransitBus.SubscribeAsync<TestMessage, TestConsumer>("default-topic");
        await _miniTransitBus.SubscribeAsync<TestMessage, TestConsumer2>("default-topic");
        await _miniTransitBus.StartProcessingAsync();

        // Act
        await _miniTransitBus.PublishAsync(message, "default-topic");

        // Allow time for processing
        await Task.Delay(100);

        // Assert
        Assert.Equal(_retryPolicy.MaxRetryCount, retryAttempts);
        Assert.Equal(1, retryAttempts2);
    }

    [Fact]
    public async Task StopProcessingAsync_ShouldCancelCurrentMessage()
    {
        // Arrange
        var message = new TestMessage() { Content = "Test Subscription Message" };
        var receivedMessages = new List<string>();

        await _miniTransitBus.SubscribeAsync<TestMessage, TestConsumer>("default-topic");
        var processingTime = TimeSpan.FromSeconds(10);
        var startTime = DateTime.UtcNow;

        TestConsumer.OnMessageReceived = async msg =>
        {
            receivedMessages.Add(msg.Message.Content);
            await Task.Delay(processingTime, msg.CancellationToken);
        };

        await _miniTransitBus.StartProcessingAsync();

        // Act
        await _miniTransitBus.PublishAsync(message);

        // Allow some time for the message to be processed
        await Task.Delay(100);
        await _miniTransitBus.StopProcessingAsync();

        // Assert
        Assert.Single(receivedMessages);
        Assert.Equal(message.Content, receivedMessages.First());
        Assert.True(DateTime.UtcNow - startTime < processingTime.Add(TimeSpan.FromSeconds(-1)));
    }
}
