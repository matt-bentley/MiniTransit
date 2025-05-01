using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MiniTransit.Serialization;
using MiniTransit.Settings;
using MiniTransit.Subscriptions;
using Moq;

namespace MiniTransit.Tests.Subscriptions
{
    public class SubscriptionRegistryTests
    {
        private readonly IServiceProvider _serviceProvider;

        public SubscriptionRegistryTests()
        {
            var services = new ServiceCollection();
            var options = Options.Create(new MiniTransitSettings
            {
                PublishTopic = "default-topic",
                SubscribeTopic = "default-subscribe-topic"
            });
            services.AddSingleton(options);
            services.AddSingleton<IMessageSerializer, JsonMessageSerializer>();
            _serviceProvider = services.BuildServiceProvider();
        }

        [Fact]
        public void AddSubscription_ShouldAddNewSubscription()
        {
            // Arrange
            var registry = new SubscriptionRegistry(_serviceProvider, Mock.Of<IPublisher>());
            var topic = "test-topic";
            var subscriptionName = "test-subscription";

            // Act
            var subscription = registry.AddSubscription<TestMessage, TestConsumer>(topic, subscriptionName) as ConsumerSubscription<TestMessage, TestConsumer>;

            // Assert
            Assert.NotNull(subscription);
            Assert.Equal(topic, subscription.Topic);
            Assert.Equal(subscriptionName, subscription.Subscription);
        }

        [Fact]
        public void AddSubscription_ShouldThrowException_WhenDuplicateSubscription()
        {
            // Arrange
            var registry = new SubscriptionRegistry(_serviceProvider, Mock.Of<IPublisher>());
            var topic = "test-topic";
            var subscriptionName = "test-subscription";
            registry.AddSubscription<TestMessage, TestConsumer>(topic, subscriptionName);

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                registry.AddSubscription<TestMessage, TestConsumer>(topic, subscriptionName));
            Assert.Contains("already registered", exception.Message);
        }

        private class TestMessage { }

        private class TestConsumer : IConsumer<TestMessage>
        {
            public Task ConsumeAsync(ConsumeContext<TestMessage> context)
            {
                throw new NotImplementedException();
            }
        }
    }
}
