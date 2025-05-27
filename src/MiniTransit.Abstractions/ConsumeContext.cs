using MiniTransit.Subscriptions;

namespace MiniTransit
{
    public class ConsumeContext<TMessage> where TMessage : class
    {
        public ConsumeContext(TMessage message,
            SubscriptionContext subscriptionContext,
            IPublisher publisher,
            CancellationToken cancellationToken)
        {
            Message = message;
            SubscriptionContext = subscriptionContext;
            Publisher = publisher;
            CancellationToken = cancellationToken;
        }

        public readonly TMessage Message;
        public readonly SubscriptionContext SubscriptionContext;
        public readonly IPublisher Publisher;
        public readonly CancellationToken CancellationToken;
    }
}