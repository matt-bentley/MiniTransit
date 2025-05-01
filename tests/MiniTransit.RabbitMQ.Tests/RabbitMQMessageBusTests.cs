using Microsoft.Extensions.DependencyInjection;
using Testcontainers.RabbitMq;

namespace MiniTransit.RabbitMQ.Tests;

public class RabbitMQMessageBusTests : IClassFixture<RabbitMqContainerFixture>
{
    private readonly RabbitMqContainer _container;

    public RabbitMQMessageBusTests(RabbitMqContainerFixture fixture)
    {
        _container = fixture.Container;
    }

    [Fact]
    public async Task GivenRabbitMqMessageBus_WhenPublish_ThenSubscriberReceive()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<TestConsumer>();
        services.AddLogging();
        services.AddMiniTransit((settings, builder) =>
        {
            builder.UseRabbitMQ(options =>
            {
                options.HostName = "localhost";
                options.Port = _container!.GetMappedPublicPort(5672);
            });
        });
        var serviceProvider = services.BuildServiceProvider();
        var message = new TestMessage() { Content = "Test Subscription Message" };
        var receivedMessages = new List<string>();

        // Act
        var bus = serviceProvider.GetRequiredService<IBus>();
        await bus.SubscribeAsync<TestMessage, TestConsumer>();

        TestConsumer.OnMessageReceived = msg =>
        {
            receivedMessages.Add(msg);
            return Task.CompletedTask;
        };

        await bus.StartProcessingAsync();

        await bus.PublishAsync(message);

        // Allow some time for the message to be processed
        await Task.Delay(1000);

        // Assert
        Assert.Single(receivedMessages);
        Assert.Equal(message.Content, receivedMessages.First());
    }

    [Fact]
    public async Task GivenRabbitMqMessageBus_WhenSchedule_ThenSubscriberReceiveDelayed()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<TestConsumer>();
        services.AddLogging();
        services.AddMiniTransit((settings, builder) =>
        {
            builder.UseRabbitMQ(options =>
            {
                options.HostName = "localhost";
                options.Port = _container!.GetMappedPublicPort(5672);
            });
        });
        var serviceProvider = services.BuildServiceProvider();
        var message = new TestMessage() { Content = "Test Subscription Message" };
        var receivedMessages = new List<string>();

        // Act
        var bus = serviceProvider.GetRequiredService<IBus>();
        await bus.SubscribeAsync<TestMessage, TestConsumer>();
        var receivedDate = DateTime.MinValue;

        TestConsumer.OnMessageReceived = msg =>
        {
            receivedMessages.Add(msg);
            receivedDate = DateTime.UtcNow;
            return Task.CompletedTask;
        };

        await bus.StartProcessingAsync();

        var publishedDate = DateTime.UtcNow;
        await bus.ScheduleAsync(message, TimeSpan.FromSeconds(1));

        // Allow some time for the message to be processed
        await Task.Delay(2000);

        // Assert
        Assert.Single(receivedMessages);
        Assert.Equal(message.Content, receivedMessages.First());
        Assert.True(receivedDate > publishedDate.Add(TimeSpan.FromSeconds(1)));
    }

    private class TestMessage
    {
        public required string Content { get; set; }
    }

    private class TestConsumer : IConsumer<TestMessage>
    {
        public static Func<string, Task> OnMessageReceived { get; set; } = _ => Task.CompletedTask;

        public Task ConsumeAsync(ConsumeContext<TestMessage> context)
        {
            return OnMessageReceived.Invoke(context.Message.Content);
        }
    }
}
