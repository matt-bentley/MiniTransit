using Testcontainers.RabbitMq;

namespace MiniTransit.RabbitMQ.Tests
{
    public class RabbitMqContainerFixture : IAsyncLifetime
    {
        public RabbitMqContainer Container { get; private set; }

        public RabbitMqContainerFixture()
        {
            Container = new RabbitMqBuilder()
                .WithImage("masstransit/rabbitmq")
                .WithName("rabbitmq")
                .WithPortBinding(5672, assignRandomHostPort: true)
                .WithEnvironment("RABBITMQ_DEFAULT_USER", "guest")
                .WithEnvironment("RABBITMQ_DEFAULT_PASS", "guest")
                .Build();
        }

        public async Task InitializeAsync()
        {
            await Container.StartAsync();
        }

        public async Task DisposeAsync()
        {
            await Container.StopAsync();
        }
    }
}
