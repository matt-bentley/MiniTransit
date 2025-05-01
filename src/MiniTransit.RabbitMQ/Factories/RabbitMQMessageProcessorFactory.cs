using MiniTransit.Exceptions;
using MiniTransit.RabbitMQ.Settings;
using Microsoft.Extensions.Options;

namespace MiniTransit.RabbitMQ.Factories
{
    public sealed class RabbitMQMessageProcessorFactory
    {
        private readonly RabbitMQConnectionFactory _connectionFactory;
        private readonly IOptions<RabbitMQMessageBusSettings> _options;
        private readonly Dictionary<string, RabbitMQMessageProcessor> _subscriptions = new();

        public RabbitMQMessageProcessorFactory(RabbitMQConnectionFactory connectionFactory,
            IOptions<RabbitMQMessageBusSettings> options)
        {
            _connectionFactory = connectionFactory;
            _options = options;
        }

        public async Task<RabbitMQMessageProcessor> CreateAsync(string topicName, string subject, string subscriptionName)
        {
            var connection = await _connectionFactory.CreateAsync();
            var channel = await connection.CreateChannelAsync();

            if (_subscriptions.ContainsKey(subscriptionName))
            {
                throw new DuplicateSubscriptionException(subscriptionName);
            }

            await channel.TopicDeclareAsync(topicName, _options.Value.SchedulingEnabled);

            var args = new Dictionary<string, object?>();

            if (_options.Value.DeadLetterOnError)
            {
                var deadLetterQueueName = $"{subscriptionName}_dlq";
                await channel.QueueDeclareAsync(queue: deadLetterQueueName, durable: _options.Value.DurableQueues, exclusive: false, autoDelete: false);
                await channel.QueueBindAsync(queue: deadLetterQueueName, exchange: topicName, routingKey: deadLetterQueueName);

                args.Add("x-dead-letter-exchange", topicName);
                args.Add("x-dead-letter-routing-key", deadLetterQueueName);
            }

            await channel.QueueDeclareAsync(queue: subscriptionName, durable: _options.Value.DurableQueues, exclusive: false, autoDelete: false, arguments: args);
            await channel.QueueBindAsync(queue: subscriptionName, exchange: topicName, routingKey: GetRoutingKey(topicName, subject));
            await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: _options.Value.PrefetchCount, global: false);

            var consumer = new RabbitMQMessageProcessor(channel, subscriptionName, _options);
            _subscriptions[subscriptionName] = consumer;
            return consumer;
        }

        private static string GetRoutingKey(string topic, string subject)
        {
            return $"{topic}/{subject}";
        }

        public async Task StartProcessingAsync()
        {
            foreach (var consumer in _subscriptions.Values)
            {
                await consumer.StartConsumingAsync();
            }
        }

        public Task StopProcessingAsync()
        {
            foreach (var consumer in _subscriptions.Values)
            {
                consumer.Dispose();
            }
            _subscriptions.Clear();
            return Task.CompletedTask;
        }

        public async ValueTask DisposeAsync()
        {
            foreach (var consumer in _subscriptions.Values)
            {
                consumer.Dispose();
            }
            await Task.CompletedTask;
        }
    }
}
