
namespace MiniTransit.Tests
{
    public class TestMessage
    {
        public required string Content { get; set; }
    }

    public class TestConsumer : IConsumer<TestMessage>
    {
        public static Func<ConsumeContext<TestMessage>, Task> OnMessageReceived { get; set; } = _ => Task.CompletedTask;

        public Task ConsumeAsync(ConsumeContext<TestMessage> context)
        {
            return OnMessageReceived.Invoke(context);
        }
    }

    public class TestConsumer2 : IConsumer<TestMessage>
    {
        public static Func<string, Task> OnMessageReceived { get; set; } = _ => Task.CompletedTask;

        public Task ConsumeAsync(ConsumeContext<TestMessage> context)
        {
            return OnMessageReceived.Invoke(context.Message.Content);
        }
    }
}
