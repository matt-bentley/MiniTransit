using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MiniTransit.Serialization;
using MiniTransit.Settings;
using MiniTransit.Subscriptions;

namespace MiniTransit
{
    public sealed class MiniTransitBus : IBus
    {
        private readonly IMessageBus _messageBus;
        private readonly SubscriptionRegistry _subscriptionRegistry;
        private readonly ILogger<MiniTransitBus> _logger;
        private readonly IOptions<MiniTransitSettings> _options;
        private readonly IMessageSerializer _serializer;

        public MiniTransitBus(IMessageBus messageBus, 
            IServiceProvider serviceProvider,
            ILogger<MiniTransitBus> logger,
            IOptions<MiniTransitSettings> options,
            IMessageSerializer serializer)
        {
            _messageBus = messageBus;
            _logger = logger;
            _options = options;
            _serializer = serializer;
            _subscriptionRegistry = new SubscriptionRegistry(serviceProvider, this);
        }

        public async Task PublishAsync<TMessage>(TMessage message)
            where TMessage : class
        {
            await PublishAsync(message, _options.Value.PublishTopic);
        }

        public async Task PublishAsync<TMessage>(TMessage message, string overrideTopic) 
            where TMessage : class
        {
            ValidateTopic(overrideTopic);
            _logger.LogInformation("Publishing {type}", typeof(TMessage).Name);
            var serializedMessage = SerializeMessage(message, overrideTopic);
            await _messageBus.PublishAsync(serializedMessage, overrideTopic, GetMessageSubject<TMessage>());
        }

        public async Task ScheduleAsync<TMessage>(TMessage message, TimeSpan delay) 
            where TMessage : class
        {
            await ScheduleAsync(message, delay, _options.Value.PublishTopic);
        }

        public async Task ScheduleAsync<TMessage>(TMessage message, TimeSpan delay, string overrideTopic)
            where TMessage : class
        {
            if(delay < TimeSpan.Zero)
            {
                throw new InvalidOperationException("Delay cannot be less than zero.");
            }
            ValidateTopic(overrideTopic);
            _logger.LogInformation("Scheduling {type} at {time}", typeof(TMessage).Name, DateTime.UtcNow + delay);
            var serializedMessage = SerializeMessage(message, overrideTopic);
            if (delay == TimeSpan.Zero)
            {
                await _messageBus.PublishAsync(serializedMessage, overrideTopic, GetMessageSubject<TMessage>());
            }
            else
            {
                await _messageBus.ScheduleAsync(serializedMessage, overrideTopic, GetMessageSubject<TMessage>(), delay);
            }
        }

        async Task IPublisher.ScheduleRetryAsync<TMessage>(MessageEnvelope<TMessage> message, TimeSpan delay)
            where TMessage : class
        {
            _logger.LogInformation("Scheduling retry for {type} at {time}", typeof(TMessage).Name, DateTime.UtcNow + delay);
            var serializedMessage = _serializer.Serialize(message);
            if (delay <= TimeSpan.Zero)
            {
                await _messageBus.PublishAsync(serializedMessage, message.SubscriptionContext!.Topic, GetMessageSubject<TMessage>());
            }
            else
            {
                await _messageBus.ScheduleAsync(serializedMessage, message.SubscriptionContext!.Topic, GetMessageSubject<TMessage>(), delay);
            }
        }

        private byte[] SerializeMessage<TMessage>(TMessage message, string topic)
            where TMessage : class
        {
            var messageEnvelope = new MessageEnvelope<TMessage>()
            {
                Message = message,
                SubscriptionContext = new SubscriptionContext(topic, null, typeof(TMessage).Name, null, 0)
            };
            return _serializer.Serialize(messageEnvelope);
        }

        private static void ValidateTopic(string topic)
        {
            if (string.IsNullOrWhiteSpace(topic))
            {
                throw new ArgumentNullException("Topic");
            }
        }

        private static string GetMessageSubject<TMessage>() where TMessage : class
        {
            return typeof(TMessage).Name;
        }

        public async Task SubscribeAsync<TMessage, TConsumer>() 
            where TMessage : class 
            where TConsumer : IConsumer<TMessage>
        {
            await SubscribeAsync<TMessage, TConsumer>(_options.Value.SubscribeTopic);
        }

        public async Task SubscribeAsync<TMessage, TConsumer>(string overrideTopic)
            where TMessage : class
            where TConsumer : IConsumer<TMessage>
        {
            var messageType = typeof(TMessage).Name;
            var consumerType = typeof(TConsumer).Name;
            var subscription = $"{messageType}.{consumerType}";

            _logger.LogInformation("Subscribing to {messageType} with {consumer}", messageType, consumerType);
            var consumerSubscription = _subscriptionRegistry.AddSubscription<TMessage, TConsumer>(overrideTopic, subscription);
            await _messageBus.SubscribeAsync(overrideTopic, GetMessageSubject<TMessage>(), subscription, consumerSubscription.HandleMessageAsync);
        }

        public async Task StartProcessingAsync()
        {
            _logger.LogInformation("Starting to consume messages");
            await _messageBus.StartProcessingAsync();
        }

        public async Task StopProcessingAsync()
        {
            _logger.LogInformation("Stopping consuming messages");
            await _messageBus.StopProcessingAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await _messageBus.DisposeAsync();
        }
    }
}