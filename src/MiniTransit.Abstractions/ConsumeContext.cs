using MiniTransit.Subscriptions;

namespace MiniTransit
{
    public class ConsumeContext<TMessage> where TMessage : class
    {
        public ConsumeContext(TMessage message,
            SubscriptionContext subscriptionContext,
            IPublisher publisher)
        {
            Message = message;
            SubscriptionContext = subscriptionContext;
            Publisher = publisher;
        }

        public readonly TMessage Message;
        public readonly SubscriptionContext SubscriptionContext;
        public readonly IPublisher Publisher;
    }
}