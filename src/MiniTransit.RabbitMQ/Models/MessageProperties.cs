using RabbitMQ.Client;

namespace MiniTransit.RabbitMQ.Models
{
    internal static class MessageProperties
    {
        internal static BasicProperties GetDefaultMessageProperties(int retryCount = 0)
        {
            return new BasicProperties
            {
                Headers = GetDefaultHeaders(retryCount)
            };
        }

        internal static IDictionary<string, object?> GetDefaultHeaders(int retryCount = 0)
        {
            return new Dictionary<string, object?>
            {
                { "retry-count", retryCount }
            };
        }
    }
}
