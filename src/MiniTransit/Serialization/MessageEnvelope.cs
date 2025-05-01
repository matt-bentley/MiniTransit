
using MiniTransit.Subscriptions;

namespace MiniTransit.Serialization
{
    public sealed class MessageEnvelope<TMessage> where TMessage : class
    {
        public required TMessage Message { get; set; }
        public SubscriptionContext? SubscriptionContext { get; set; }

        public void SetSubscriptionContext(string topic, string subscription, string messageType, string consumer, int retryCount)
        {
            SubscriptionContext = new SubscriptionContext(topic, subscription, messageType, consumer, retryCount);
        }
    }
}
