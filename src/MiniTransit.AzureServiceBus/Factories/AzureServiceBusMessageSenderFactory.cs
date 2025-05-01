using Azure.Messaging.ServiceBus;
using MiniTransit.AzureServiceBus.Settings;
using Microsoft.Extensions.Options;

namespace MiniTransit.AzureServiceBus.Factories
{
    public sealed class AzureServiceBusMessageSenderFactory
    {
        private readonly string _connectionString;
        private ServiceBusClient _client;
        private readonly object _lock = new();
        private readonly Dictionary<string, ServiceBusSender> _senders = new();

        public AzureServiceBusMessageSenderFactory(IOptions<AzureServiceBusMessageBusSettings> options)
        {
            if (string.IsNullOrEmpty(options.Value.ConnectionString))
            {
                throw new ArgumentNullException(nameof(options.Value.ConnectionString));
            }
            _connectionString = options.Value.ConnectionString;
            _client = new ServiceBusClient(options.Value.ConnectionString);
        }

        public AzureServiceBusMessageSender Create(string topicName)
        {
            lock (_lock)
            {
                if (!_senders.TryGetValue(topicName, out ServiceBusSender? sender))
                {
                    sender = GetClient().CreateSender(topicName);
                    _senders.Add(topicName, sender);
                }
                return new AzureServiceBusMessageSender(sender);
            }
        }

        private ServiceBusClient GetClient()
        {
            if(_client == null || _client.IsClosed)
            {
                _client = new ServiceBusClient(_connectionString);
            }
            return _client;
        }

        public async ValueTask DisposeAsync()
        {
            foreach(var processor in _senders.Values)
            {
                if (!processor.IsClosed)
                {
                    await processor.DisposeAsync();
                }
            }
            await _client.DisposeAsync();
        }
    }
}
