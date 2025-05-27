using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MiniTransit.Policies;
using MiniTransit.Serialization;

namespace MiniTransit.Subscriptions
{
    internal sealed class ConsumerSubscription<TMessage, TConsumer> : IEquatable<ConsumerSubscription<TMessage, TConsumer>>, IConsumerSubscription
         where TMessage : class
         where TConsumer : IConsumer<TMessage>
    {
        private readonly Func<TConsumer, ConsumeContext<TMessage>, Task> _consumerHandler;
        private readonly IServiceProvider _serviceProvider;
        private readonly IPublisher _publisher;
        private readonly IMessageSerializer _serializer;
        public readonly string Topic;
        public readonly string Subscription;
        public readonly string MessageType;
        public readonly string ConsumerType;
        private readonly CancellationTokenSource _cts;

        public ConsumerSubscription(IServiceProvider serviceProvider,
            IPublisher publisher,
            string topic,
            string subscription,
            CancellationTokenSource cts)
        {
            _publisher = publisher;
            Topic = topic;
            Subscription = subscription;
            MessageType = typeof(TMessage).Name;
            ConsumerType = typeof(TConsumer).Name;
            var methodInfo = typeof(IConsumer<TMessage>).GetMethod("ConsumeAsync")!;
            _consumerHandler = (Func<TConsumer, ConsumeContext<TMessage>, Task>)Delegate.CreateDelegate(
                typeof(Func<TConsumer, ConsumeContext<TMessage>, Task>), methodInfo);

            _serviceProvider = serviceProvider;
            _serializer = serviceProvider.GetRequiredService<IMessageSerializer>();
            _cts = cts;
        }

        public async Task HandleMessageAsync(byte[] message)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<ConsumerSubscription<TMessage, TConsumer>>>();
                var messageEnvelope = _serializer.Deserialize<TMessage>(message);
                if (IsFromDifferentConsumerRetry(messageEnvelope))
                {
                    logger.LogWarning("Ignoring message - message has been re-queued by a different consumer: {consumer}", messageEnvelope.SubscriptionContext!.Consumer);
                    return;
                }
                var retryCount = messageEnvelope.SubscriptionContext?.RetryCount ?? 0;
                messageEnvelope.SetSubscriptionContext(Topic, Subscription, MessageType, ConsumerType, retryCount);
                try
                {
                    var consumer = scope.ServiceProvider.GetRequiredService<TConsumer>();                
                    var context = new ConsumeContext<TMessage>(messageEnvelope.Message!, messageEnvelope.SubscriptionContext!, _publisher, _cts.Token);
                    await _consumerHandler.Invoke(consumer, context);
                }
                catch (Exception ex)
                {
                    retryCount++;
                    var retryPolicy = scope.ServiceProvider.GetService<IRetryPolicy>();
                    if(retryPolicy == null || !retryPolicy.ShouldRetry(retryCount, ex))
                    {
                        logger.LogError(ex, "Message processing failed after {retryCount} attempts. Discarding message.", retryCount);
                        if (retryPolicy?.ShouldThrow(ex) ?? true)
                        {
                            throw;
                        }
                    }
                    else
                    {
                        messageEnvelope.SubscriptionContext!.Retry(ex);
                        var delay = retryPolicy.GetRetryDelay(retryCount);                
                        await _publisher.ScheduleRetryAsync(messageEnvelope, delay);
                        logger.LogWarning(ex, "Message processing failed. Retrying attempt {RetryCount} after {Delay} seconds.", retryCount, delay.TotalSeconds);
                    }
                }
            }
        }

        private bool IsFromDifferentConsumerRetry(MessageEnvelope<TMessage> messageEnvelope)
        {
            return messageEnvelope.SubscriptionContext != null
                && !string.IsNullOrEmpty(messageEnvelope.SubscriptionContext.Consumer)
                && !messageEnvelope.SubscriptionContext.Consumer.Equals(ConsumerType, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as ConsumerSubscription<TMessage, TConsumer>);
        }

        public bool Equals(ConsumerSubscription<TMessage, TConsumer>? other)
        {
            return other != null && ConsumerType.Equals(other.ConsumerType);
        }

        public override int GetHashCode()
        {
            return ConsumerType.GetHashCode();
        }
    }
}
