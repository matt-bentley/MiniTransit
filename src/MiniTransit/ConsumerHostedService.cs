using Microsoft.Extensions.Hosting;

namespace MiniTransit
{
    public sealed class ConsumerHostedService<TMessage, TConsumer> : IHostedLifecycleService
        where TConsumer : class, IConsumer<TMessage>
        where TMessage : class
    {
        private readonly IBus _bus;

        public ConsumerHostedService(IBus bus)
        {
            _bus = bus;
        }

        public Task StartingAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _bus.SubscribeAsync<TMessage, TConsumer>();
        }

        public async Task StartedAsync(CancellationToken cancellationToken)
        {
            await _bus.StartProcessingAsync();
        }

        public Task StoppingAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _bus.StopProcessingAsync();
        }

        public Task StoppedAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
