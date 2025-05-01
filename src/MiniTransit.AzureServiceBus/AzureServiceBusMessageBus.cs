using Microsoft.Extensions.Logging;
using MiniTransit.AzureServiceBus.Factories;

namespace MiniTransit.AzureServiceBus
{
    public class AzureServiceBusMessageBus : IMessageBus
    {
        private readonly ILogger<AzureServiceBusMessageBus> _logger;
        private readonly AzureServiceBusMessageProcessorFactory _processorFactory;
        private readonly AzureServiceBusMessageSenderFactory _senderFactory;

        public AzureServiceBusMessageBus(AzureServiceBusMessageProcessorFactory processorFactory,
            AzureServiceBusMessageSenderFactory senderFactory,
            ILogger<AzureServiceBusMessageBus> logger)
        {
            _processorFactory = processorFactory;
            _senderFactory = senderFactory;
            _logger = logger;
        }

        public async Task PublishAsync(byte[] message, string topic, string subject)
        {
            var sender = _senderFactory.Create(topic);
            _logger.LogDebug("Sending message to: {topicName}", topic);
            await sender.SendAsync(message, subject);
        }

        public async Task ScheduleAsync(byte[] message, string topic, string subject, TimeSpan delay)
        {
            var sender = _senderFactory.Create(topic);
            var scheduledEnqueueTime = DateTime.UtcNow + delay;
            _logger.LogDebug("Scheduling message for: {topicName} at {time}", topic, scheduledEnqueueTime);
            await sender.ScheduleAsync(message, subject, scheduledEnqueueTime);
        }

        public async Task SubscribeAsync(string topic, string subject, string subscriptionName, Func<byte[], Task> handler)
        {
            _logger.LogDebug("Subscribing to '{subscription}' on topic '{topic}'", subscriptionName, topic);
            var processor = await _processorFactory.CreateAsync(topic, subject, subscriptionName);
            processor.RegisterHandler(handler, _logger);
        }

        public async Task StartProcessingAsync()
        {
            await _processorFactory.StartProcessingAsync();
        }

        public async Task StopProcessingAsync()
        {
            await _processorFactory.StopProcessingAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await _processorFactory.DisposeAsync();
            await _senderFactory.DisposeAsync();
        }
    }
}