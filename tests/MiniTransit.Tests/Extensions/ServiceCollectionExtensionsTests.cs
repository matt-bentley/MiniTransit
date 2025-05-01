using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MiniTransit.Policies;
using MiniTransit.Tests.Policies;

namespace MiniTransit.Tests.Extensions
{
    public class ServiceCollectionExtensionsTests
    {
        [Fact]
        public async Task GivenServiceCollection_WhenAddMiniTransit_ThenRegister()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddTransient<TestConsumer>();
            services.AddLogging();
            services.AddMiniTransit((settings, builder) =>
            {
                builder.UseInMemory();
            });
            var serviceProvider = services.BuildServiceProvider();
            var message = new TestMessage() { Content = "Test Subscription Message" };
            var receivedMessages = new List<string>();

            // Act
            var bus = serviceProvider.GetRequiredService<IBus>();
            await bus.SubscribeAsync<TestMessage, TestConsumer>();

            TestConsumer.OnMessageReceived = msg =>
            {
                receivedMessages.Add(msg);
                return Task.CompletedTask;
            };

            await bus.StartProcessingAsync();

            await bus.PublishAsync(message);

            // Allow some time for the message to be processed
            await Task.Delay(100);

            // Assert
            Assert.Single(receivedMessages);
            Assert.Equal(message.Content, receivedMessages.First());
        }

        [Fact]
        public async Task GivenServiceCollection_WhenAddMiniTransitAndConsumer_ThenRegister()
        {
            // Arrange
            var host = Host.CreateDefaultBuilder()
                    .ConfigureServices(services =>
                    {
                        services.AddMiniTransit((settings, builder) =>
                        {
                            builder.UseInMemory();
                            builder.AddConsumer<TestConsumer>();
                            builder.AddConsumer<TestConsumer2>();
                        });
                    })
                    .Build();

            var message = new TestMessage() { Content = "Test Subscription Message" };
            var receivedMessages = new List<string>();
            TestConsumer.OnMessageReceived = msg =>
            {
                receivedMessages.Add(msg);
                return Task.CompletedTask;
            };

            // Act
            await host.StartAsync();

            try
            {
                var bus = host.Services.GetRequiredService<IBus>();

                await bus.PublishAsync(message);

                await Task.Delay(200);
            }
            finally
            {
                await host.StopAsync();
            }            

            // Assert
            Assert.Single(receivedMessages);
            Assert.Equal(message.Content, receivedMessages.First());
        }

        [Fact]
        public void GivenServiceCollection_WhenAddMiniTransitWithRetryPolicy_ThenRegistersRetryPolicy()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddMiniTransit((settings, builder) =>
            {
                builder.UseInMemory();
                builder.UseRetry(retry =>
                {
                    retry.Immediate(3);
                    retry.Ignore<ArgumentException>();
                });
            });

            var serviceProvider = services.BuildServiceProvider();
            var retryPolicy = serviceProvider.GetRequiredService<IRetryPolicy>();

            // Assert
            Assert.IsType<DefaultRetryPolicy>(retryPolicy);
            var defaultPolicy = (DefaultRetryPolicy)retryPolicy;
            Assert.Equal(3, defaultPolicy.RetryIntervals.Length);
            Assert.All(defaultPolicy.RetryIntervals, interval => Assert.Equal(TimeSpan.Zero, interval));
            Assert.Contains(typeof(ArgumentException), defaultPolicy.IgnoreExceptions);
        }

        [Fact]
        public void GivenServiceCollection_WhenAddMiniTransitWithIntervalRetryPolicy_ThenRegistersRetryPolicy()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddMiniTransit((settings, builder) =>
            {
                builder.UseInMemory();
                builder.UseRetry(retry =>
                {
                    retry.Interval(3, TimeSpan.FromSeconds(2));
                });
            });

            var serviceProvider = services.BuildServiceProvider();
            var retryPolicy = serviceProvider.GetRequiredService<IRetryPolicy>();

            // Assert
            Assert.IsType<DefaultRetryPolicy>(retryPolicy);
            var defaultPolicy = (DefaultRetryPolicy)retryPolicy;
            Assert.Equal(3, defaultPolicy.RetryIntervals.Length);
            Assert.All(defaultPolicy.RetryIntervals, interval => Assert.Equal(TimeSpan.FromSeconds(2), interval));
        }

        [Fact]
        public void GivenServiceCollection_WhenAddMiniTransitWithCustomIntervalsRetryPolicy_ThenRegistersRetryPolicy()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddMiniTransit((settings, builder) =>
            {
                builder.UseInMemory();
                builder.UseRetry(retry =>
                {
                    retry.Intervals(new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(5) });
                });
            });

            var serviceProvider = services.BuildServiceProvider();
            var retryPolicy = serviceProvider.GetRequiredService<IRetryPolicy>();

            // Assert
            Assert.IsType<DefaultRetryPolicy>(retryPolicy);
            var defaultPolicy = (DefaultRetryPolicy)retryPolicy;
            Assert.Equal(3, defaultPolicy.RetryIntervals.Length);
            Assert.Equal(TimeSpan.FromSeconds(1), defaultPolicy.RetryIntervals[0]);
            Assert.Equal(TimeSpan.FromSeconds(3), defaultPolicy.RetryIntervals[1]);
            Assert.Equal(TimeSpan.FromSeconds(5), defaultPolicy.RetryIntervals[2]);
        }

        [Fact]
        public void GivenServiceCollection_WhenAddMiniTransitWithExponentialRetryPolicy_ThenRegistersRetryPolicy()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddMiniTransit((settings, builder) =>
            {
                builder.UseInMemory();
                builder.UseRetry(retry =>
                {
                    retry.Exponential(3, TimeSpan.FromMilliseconds(500));
                });
            });

            var serviceProvider = services.BuildServiceProvider();
            var retryPolicy = serviceProvider.GetRequiredService<IRetryPolicy>();

            // Assert
            Assert.IsType<DefaultRetryPolicy>(retryPolicy);
            var defaultPolicy = (DefaultRetryPolicy)retryPolicy;
            Assert.Equal(3, defaultPolicy.RetryIntervals.Length);
            Assert.Equal(TimeSpan.FromMilliseconds(500), defaultPolicy.RetryIntervals[0]);
            Assert.Equal(TimeSpan.FromMilliseconds(1000), defaultPolicy.RetryIntervals[1]);
            Assert.Equal(TimeSpan.FromMilliseconds(2000), defaultPolicy.RetryIntervals[2]);
        }

        [Fact]
        public void GivenServiceCollection_WhenAddMiniTransitWithCustomRetryPolicy_ThenRegistersCustomPolicy()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddMiniTransit((settings, builder) =>
            {
                builder.UseInMemory();
                builder.UseRetry(retry =>
                {
                    retry.Custom<MockRetryPolicy>();
                });
            });

            var serviceProvider = services.BuildServiceProvider();
            var retryPolicy = serviceProvider.GetRequiredService<IRetryPolicy>();

            // Assert
            Assert.IsType<MockRetryPolicy>(retryPolicy);
        }

        private class TestMessage
        {
            public required string Content { get; set; }
        }

        private class TestConsumer : IConsumer<TestMessage>
        {
            public static Func<string, Task> OnMessageReceived { get; set; } = _ => Task.CompletedTask;

            public Task ConsumeAsync(ConsumeContext<TestMessage> context)
            {
                return OnMessageReceived.Invoke(context.Message.Content);
            }
        }

        private class TestConsumer2 : IConsumer<TestMessage>
        {
            public static Func<string, Task> OnMessageReceived { get; set; } = _ => Task.CompletedTask;

            public Task ConsumeAsync(ConsumeContext<TestMessage> context)
            {
                return OnMessageReceived.Invoke(context.Message.Content);
            }
        }
    }
}
