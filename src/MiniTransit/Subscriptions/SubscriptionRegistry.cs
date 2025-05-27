
namespace MiniTransit.Subscriptions
{
    internal sealed class SubscriptionRegistry
    {
        private readonly Dictionary<string, IConsumerSubscription> _subscriptions;
        private readonly IServiceProvider _serviceProvider;
        private readonly IPublisher _publisher;
        private readonly CancellationTokenSource _cts;

        public SubscriptionRegistry(IServiceProvider serviceProvider, 
            IPublisher publisher,
            CancellationTokenSource cts)
        {
            _subscriptions = [];
            _serviceProvider = serviceProvider;
            _publisher = publisher;
            _cts = cts;
        }

        public IConsumerSubscription AddSubscription<TMessage, TConsumer>(string topic, string subscriptionName)
            where TMessage : class
            where TConsumer : IConsumer<TMessage>
        {
            var subscriptionKey = $"{topic}.{subscriptionName}";
            if (_subscriptions.ContainsKey(subscriptionKey))
            {
                throw new ArgumentException($"Subscription {subscriptionName} already registered in topic {topic}", nameof(subscriptionName));
            }

            var subscription = new ConsumerSubscription<TMessage, TConsumer>(_serviceProvider, _publisher, topic, subscriptionName, _cts);
            _subscriptions.Add(subscriptionKey, subscription);
            return subscription;
        }
    }
}
