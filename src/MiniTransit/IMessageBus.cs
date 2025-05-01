namespace MiniTransit
{
    public interface IMessageBus : IAsyncDisposable
    {
        Task PublishAsync(byte[] message, string topic, string subject);
        Task ScheduleAsync(byte[] message, string topic, string subject, TimeSpan delay);
        Task SubscribeAsync(string topic, string subject, string subscriptionName, Func<byte[], Task> handler);
        Task StartProcessingAsync();
        Task StopProcessingAsync();
    }
}