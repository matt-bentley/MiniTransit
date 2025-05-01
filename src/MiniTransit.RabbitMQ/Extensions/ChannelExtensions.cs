using RabbitMQ.Client;

namespace MiniTransit.RabbitMQ
{
    internal static class ChannelExtensions
    {
        internal async static Task TopicDeclareAsync(this IChannel channel, string topicName, bool schedulingEnabled)
        {
            if (schedulingEnabled)
            {
                await channel.ExchangeDeclareAsync(topicName, "x-delayed-message",
                    arguments: new Dictionary<string, object?> { { "x-delayed-type", ExchangeType.Topic } });
            }
            else
            {
                await channel.ExchangeDeclareAsync(exchange: topicName, type: ExchangeType.Topic);
            }
        }
    }
}
