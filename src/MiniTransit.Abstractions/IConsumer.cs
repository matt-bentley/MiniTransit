namespace MiniTransit
{
    public interface IConsumer
    {
        
    }

    public interface IConsumer<TMessage> : IConsumer where TMessage : class
    {
        Task ConsumeAsync(ConsumeContext<TMessage> context);
    }
}