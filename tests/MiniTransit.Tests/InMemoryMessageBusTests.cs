using Microsoft.Extensions.Logging;
using Moq;
using System.Text;

namespace MiniTransit.Tests
{
    public class InMemoryMessageBusTests
    {
        private readonly Mock<ILogger<InMemoryMessageBus>> _loggerMock = new();
        private InMemoryMessageBus CreateBus() => new(_loggerMock.Object);

        [Fact]
        public async Task PublishAsync_SendsMessageToSubscriber()
        {
            var bus = CreateBus();
            string? received = null;
            var msg = "TestMessage";

            await bus.SubscribeAsync("topic1", "subj1", "subname", async v => { received = Encoding.UTF8.GetString(v); await Task.CompletedTask; });
            await bus.StartProcessingAsync();
            await bus.PublishAsync(Encoding.UTF8.GetBytes(msg), "topic1", "subj1");

            // Wait for delivery  
            await Task.Delay(100);

            Assert.Equal(msg, received);
        }

        [Fact]
        public async Task MultipleSubscribers_ReceivePublishedMessages()
        {
            var bus = CreateBus();
            int received1 = 0, received2 = 0;

            await bus.SubscribeAsync("topic", "subject", "sub1", async m => 
            { 
                received1++; 
                await Task.CompletedTask; 
            });
            await bus.SubscribeAsync("topic", "subject", "sub2", async m => 
            { 
                received2++; 
                await Task.CompletedTask; 
            });

            await bus.StartProcessingAsync();

            await bus.PublishAsync(Encoding.UTF8.GetBytes("a"), "topic", "subject");
            await bus.PublishAsync(Encoding.UTF8.GetBytes("b"), "topic", "subject");

            await Task.Delay(200);

            Assert.Equal(2, received1);
            Assert.Equal(2, received2);
        }

        [Fact]
        public async Task ScheduleAsync_DelaysDelivery()
        {
            var bus = CreateBus();
            var received = false;
            await bus.SubscribeAsync("topic", "subject", "sub", async m => { received = true; await Task.CompletedTask; });
            await bus.StartProcessingAsync();

            var sw = System.Diagnostics.Stopwatch.StartNew();
            await bus.ScheduleAsync(Encoding.UTF8.GetBytes("scheduled"), "topic", "subject", TimeSpan.FromMilliseconds(150));
            await Task.Delay(100);

            Assert.False(received); // Should not be delivered yet  

            await Task.Delay(100);

            Assert.True(received); // Should have delivered  
        }

        [Fact]
        public async Task SubscribeAfterStart_ActivatesImmediately()
        {
            var bus = CreateBus();
            await bus.StartProcessingAsync();

            string? got = null;
            await bus.SubscribeAsync("t", "s", "n", async m => got = Encoding.UTF8.GetString(m));

            await bus.PublishAsync(Encoding.UTF8.GetBytes("hi!"), "t", "s");
            await Task.Delay(100);
            Assert.Equal("hi!", got);
        }

        [Fact]
        public async Task StopProcessingAsync_CancelsSubscriptions()
        {
            var bus = CreateBus();

            int cnt = 0;
            await bus.SubscribeAsync("t", "s", "n", async m => cnt++);
            await bus.StartProcessingAsync();

            await bus.PublishAsync(Encoding.UTF8.GetBytes("1"), "t", "s");

            await Task.Delay(100);

            await bus.StopProcessingAsync();

            await bus.PublishAsync(Encoding.UTF8.GetBytes("2"), "t", "s");

            await Task.Delay(50);

            Assert.InRange(cnt, 1, 1); // Only the first delivered before stop  
        }

        [Fact]
        public async Task DisposeAsync_CleansUpResources()
        {
            var bus = CreateBus();
            await bus.StartProcessingAsync();
            await bus.DisposeAsync();

            // Already disposed call doesn't throw  
            await bus.DisposeAsync();
        }
    }
}
