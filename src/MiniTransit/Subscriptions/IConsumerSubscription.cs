
namespace MiniTransit.Subscriptions
{
    internal interface IConsumerSubscription
    {
        Task HandleMessageAsync(byte[] message);
    }
}
