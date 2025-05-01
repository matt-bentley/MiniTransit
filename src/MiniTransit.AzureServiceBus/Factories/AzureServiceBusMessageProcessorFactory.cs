using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using MiniTransit.AzureServiceBus.Settings;
using Microsoft.Extensions.Options;
using MiniTransit.Exceptions;

namespace MiniTransit.AzureServiceBus.Factories
{
    public sealed class AzureServiceBusMessageProcessorFactory
    {
        private readonly ServiceBusClient _client;
        private readonly AzureServiceBusMessageBusSettings _settings;
        private readonly Dictionary<string, ServiceBusProcessor> _subscriptions = new Dictionary<string, ServiceBusProcessor>();
        private readonly ServiceBusProcessorOptions _receiverOptions;
        private readonly ServiceBusAdministrationClient _adminClient;
        private const string DEFAULT_FILTER_RULE_NAME = "$Default";

        public AzureServiceBusMessageProcessorFactory(IOptions<AzureServiceBusMessageBusSettings> options)
        {
            _settings = options.Value;
            _client = new ServiceBusClient(options.Value.ConnectionString);
            _adminClient = new ServiceBusAdministrationClient(options.Value.ConnectionString);
            _receiverOptions = new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = false,
                MaxConcurrentCalls = options.Value.MaxConcurrentCalls,
                ReceiveMode = ServiceBusReceiveMode.PeekLock,
                MaxAutoLockRenewalDuration = TimeSpan.FromSeconds(options.Value.MaxAutoLockRenewalDurationSeconds)
            };
        }

        public async Task<AzureServiceBusMessageProcessor> CreateAsync(string topicName, string subject, string subscription)
        {
            var subscriptionKey = GetSubscriptionKey(topicName, subscription);
            if (_subscriptions.ContainsKey(subscriptionKey))
            {
                throw new DuplicateSubscriptionException(subscriptionKey);
            }

            if (_settings.CreateSubscriptions)
            {
                await CreateSubscriptionIfNotExistsAsync(topicName, subject, subscription);
            }
            var processor = _client.CreateProcessor(topicName, subscription, _receiverOptions);
            _subscriptions.Add(subscriptionKey, processor);
            return new AzureServiceBusMessageProcessor(processor, topicName, subscription);
        }

        private string GetSubscriptionKey(string topicName, string subscription)
        {
            return $"{topicName}/{subscription}";
        }

        private async Task CreateSubscriptionIfNotExistsAsync(string topicName, string subject, string subscription)
        {
            if (!await _adminClient.SubscriptionExistsAsync(topicName, subscription))
            {
                var createOptions = new CreateSubscriptionOptions(topicName, subscription)
                {
                    MaxDeliveryCount = _settings.MaxDeliveryCount,
                    DefaultMessageTimeToLive = TimeSpan.FromDays(_settings.DefaultMessageTimeToLiveDays),
                    LockDuration = TimeSpan.FromSeconds(_settings.LockDurationSeconds),
                    DeadLetteringOnMessageExpiration = _settings.DeadLetteringOnMessageExpiration
                };
                await _adminClient.CreateSubscriptionAsync(createOptions);
                if (_settings.SubjectFiltering)
                {
                    await EnableSubjectFilteringAsync(topicName, subject, subscription);
                }
            }
        }

        private async Task EnableSubjectFilteringAsync(string topicName, string subject, string subscription)
        {
            if (await _adminClient.RuleExistsAsync(topicName, subscription, DEFAULT_FILTER_RULE_NAME))
            {
                await _adminClient.DeleteRuleAsync(topicName, subscription, DEFAULT_FILTER_RULE_NAME);
            }
            if (!await _adminClient.RuleExistsAsync(topicName, subscription, subject))
            {
                var filter = new CorrelationRuleFilter()
                {
                    Subject = subject
                };
                var rule = new CreateRuleOptions(subject, filter);
                await _adminClient.CreateRuleAsync(topicName, subscription, rule);
            }
        }

        public async Task StartProcessingAsync()
        {
            foreach (var processor in _subscriptions.Values)
            {
                if (!processor.IsProcessing)
                {
                    await processor.StartProcessingAsync();
                }
            }
        }

        public async Task StopProcessingAsync()
        {
            foreach (var processor in _subscriptions.Values)
            {
                if (processor.IsProcessing)
                {
                    await processor.StopProcessingAsync();
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            await StopProcessingAsync();
            foreach (var processor in _subscriptions.Values)
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
