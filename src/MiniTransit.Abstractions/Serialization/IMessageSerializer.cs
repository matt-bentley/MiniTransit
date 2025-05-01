
namespace MiniTransit.Serialization
{
    public interface IMessageSerializer
    {
        MessageEnvelope<TMessage> Deserialize<TMessage>(byte[] message) where TMessage : class;
        byte[] Serialize<TMessage>(MessageEnvelope<TMessage> @event) where TMessage : class;
    }
}
