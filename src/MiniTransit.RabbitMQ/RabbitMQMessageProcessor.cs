using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MiniTransit.RabbitMQ.Settings;
using MiniTransit.RabbitMQ.Models;

namespace MiniTransit.RabbitMQ
{
    public sealed class RabbitMQMessageProcessor : IDisposable
    {
        private readonly IChannel _channel;
        private readonly string QueueName;
        private readonly AsyncEventingBasicConsumer _consumer;
        private readonly IOptions<RabbitMQMessageBusSettings> _options;
        private Func<byte[], Task>? _handler;
        private ILogger? _logger;

        public RabbitMQMessageProcessor(IChannel channel, 
            string queueName,
            IOptions<RabbitMQMessageBusSettings> options)
        {
            _channel = channel;
            _consumer = new AsyncEventingBasicConsumer(channel);
            QueueName = queueName;
            _options = options;
        }

        public void RegisterHandler(Func<byte[], Task> handler, ILogger logger)
        {
            _handler = handler;
            _logger = logger;
            _consumer.ReceivedAsync += OnMessageReceivedAsync;
        }

        private async Task OnMessageReceivedAsync(object model, BasicDeliverEventArgs ea)
        {
            var consumer = (AsyncEventingBasicConsumer)model;
            _logger!.LogDebug("Processing message for: {queue}", ea.RoutingKey);
            if (_options.Value.AutoAck)
            {
                await consumer.Channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            var body = ea.Body.ToArray();

            try
            {
                await _handler!.Invoke(body);
                if (!_options.Value.AutoAck)
                {
                    await consumer.Channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                }
            }
            catch (Exception ex)
            {
                _logger!.LogError(ex, "Error processing message - {ex}", ex.ToString());
                var retryCount = ea.BasicProperties.Headers != null && ea.BasicProperties.Headers.ContainsKey("retry-count")
                    ? Convert.ToInt32(ea.BasicProperties.Headers["retry-count"]) : 0;
                retryCount++;

                if (ea.BasicProperties.Headers == null || retryCount >= _options.Value.MaxDeliveryCount)
                {
                    if (!_options.Value.AutoAck)
                    {
                        await consumer.Channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                    }
                }
                else
                {
                    var props = MessageProperties.GetDefaultMessageProperties(retryCount);
                    await consumer.Channel.BasicPublishAsync(exchange: ea.Exchange,
                                         routingKey: ea.RoutingKey,
                                         mandatory: false,
                                         basicProperties: props,
                                         body: body);
                    if (!_options.Value.AutoAck)
                    {
                        await consumer.Channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                    }
                }
            }
        }

        public async Task StartConsumingAsync()
        {
            await _channel.BasicConsumeAsync(QueueName, autoAck: false, consumer: _consumer);
        }

        public void Dispose()
        {
            _channel.Dispose();
        }
    }
}
