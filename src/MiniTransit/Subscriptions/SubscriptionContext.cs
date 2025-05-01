
namespace MiniTransit.Subscriptions
{
    public sealed class SubscriptionContext
    {
        public SubscriptionContext(string topic, 
            string? subscription, 
            string messageType, 
            string? consumer, 
            int retryCount)
        {
            Topic = topic;
            Subscription = subscription;
            MessageType = messageType;
            Consumer = consumer;
            RetryCount = retryCount;
        }

        public string Topic { get; set; }
        public string? Subscription { get; set; }
        public string MessageType { get; set; }
        public string? Consumer { get; set; }
        public int RetryCount { get; set; }
        public string? Error { get; set; }

        public void Retry(Exception exception)
        {
            Error = exception.ToString();
            RetryCount++;
        }
    }
}
