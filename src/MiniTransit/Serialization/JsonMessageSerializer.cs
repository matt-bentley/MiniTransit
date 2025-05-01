using System.Text.Json;

namespace MiniTransit.Serialization
{
    public sealed class JsonMessageSerializer : IMessageSerializer
    {
        public MessageEnvelope<TMessage> Deserialize<TMessage>(byte[] message) where TMessage : class
        {
            return JsonSerializer.Deserialize<MessageEnvelope<TMessage>>(message) ?? throw new InvalidOperationException("Failed to deserialize message");
        }

        public byte[] Serialize<TMessage>(MessageEnvelope<TMessage> @event) where TMessage : class
        {
            return JsonSerializer.SerializeToUtf8Bytes(@event);
        }
    }
}
