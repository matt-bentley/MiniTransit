using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MiniTransit.RabbitMQ.Factories;
using MiniTransit.RabbitMQ.Models;
using MiniTransit.RabbitMQ.Settings;

namespace MiniTransit.RabbitMQ
{
    public class RabbitMQMessageBus : IMessageBus
    {
        private readonly ILogger<RabbitMQMessageBus> _logger;
        private readonly RabbitMQConnectionFactory _connectionFactory;
        private readonly IOptions<RabbitMQMessageBusSettings> _options;
        private readonly RabbitMQMessageProcessorFactory _processorFactory;

        public RabbitMQMessageBus(ILogger<RabbitMQMessageBus> logger,
            RabbitMQConnectionFactory connectionFactory,
            IOptions<RabbitMQMessageBusSettings> options,
            RabbitMQMessageProcessorFactory processorFactory)
        {
            _logger = logger;
            _connectionFactory = connectionFactory;
            _options = options;
            _processorFactory = processorFactory;
        }

        public async Task PublishAsync(byte[] message, string topic, string subject)
        {
            _logger.LogDebug("Sending message to: {topicName}", topic);
            await PublishMessageAsync(message, subject, topic);
        }

        public async Task ScheduleAsync(byte[] message, string topic, string subject, TimeSpan delay)
        {
            if (!_options.Value.SchedulingEnabled)
            {
                _logger.LogWarning("Scheduling not enabled for RabbitMQ - publishing message without delay");
            }
            var scheduledEnqueueTime = DateTime.UtcNow + delay;
            _logger.LogDebug("Scheduling message for: {topicName} at {time}", topic, scheduledEnqueueTime);
            await PublishMessageAsync(message, subject, topic, scheduledEnqueueTime);
        }

        private async Task PublishMessageAsync(byte[] message, string subject, string topic, DateTime? scheduledEnqueueTime = null)
        {
            var connection = await _connectionFactory.CreateAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.TopicDeclareAsync(topic, _options.Value.SchedulingEnabled);

            var props = MessageProperties.GetDefaultMessageProperties();

            if (_options.Value.DefaultMessageTimeToLiveDays > 0)
            {
                var ttlMilliseconds = _options.Value.DefaultMessageTimeToLiveDays * 24 * 60 * 60 * 1000;
                props.Expiration = ttlMilliseconds.ToString();
            }

            if (scheduledEnqueueTime.HasValue && scheduledEnqueueTime.Value > DateTime.UtcNow)
            {
                props.Headers!["x-delay"] = (int)(scheduledEnqueueTime.Value - DateTime.UtcNow).TotalMilliseconds;
            }

            await channel.BasicPublishAsync(exchange: topic,
                                 routingKey: GetRoutingKey(topic, subject),
                                 mandatory: false,
                                 body: message,
                                 basicProperties: props);
        }

        private static string GetRoutingKey(string topic, string subject)
        {
            return $"{topic}/{subject}";
        }

        public async Task SubscribeAsync(string topic, string subject, string subscriptionName, Func<byte[], Task> handler)
        {
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
            _connectionFactory.Dispose();
        }
    }
}