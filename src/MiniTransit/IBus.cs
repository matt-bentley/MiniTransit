using MiniTransit.Serialization;

namespace MiniTransit
{
    public interface IBus : IPublisher, IAsyncDisposable
    {
        Task SubscribeAsync<TMessage, TConsumer>() where TMessage : class where TConsumer : IConsumer<TMessage>;
        Task SubscribeAsync<TMessage, TConsumer>(string overrideTopic) where TMessage : class where TConsumer : IConsumer<TMessage>;
        Task StartProcessingAsync();
        Task StopProcessingAsync();
    }

    public interface IPublisher
    {
        Task PublishAsync<TMessage>(TMessage message) where TMessage : class;
        Task PublishAsync<TMessage>(TMessage message, string overrideTopic) where TMessage : class;
        Task ScheduleAsync<TMessage>(TMessage message, TimeSpan delay) where TMessage : class;
        Task ScheduleAsync<TMessage>(TMessage message, TimeSpan delay, string overrideTopic) where TMessage : class;
        internal Task ScheduleRetryAsync<TMessage>(MessageEnvelope<TMessage> message, TimeSpan delay) where TMessage : class;
    }
}