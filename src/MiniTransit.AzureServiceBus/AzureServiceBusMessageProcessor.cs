using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;

namespace MiniTransit.AzureServiceBus
{
    public sealed class AzureServiceBusMessageProcessor
    {
        private readonly ServiceBusProcessor _serviceBusProcessor;
        public readonly string TopicName;
        public readonly string Subscription;
        private Func<byte[], Task>? _handler;
        private ILogger? _logger;

        public AzureServiceBusMessageProcessor(ServiceBusProcessor serviceBusProcessor, 
            string topicName, 
            string subscription)
        {
            _serviceBusProcessor = serviceBusProcessor;
            TopicName = topicName;
            Subscription = subscription;
        }

        public void RegisterHandler(Func<byte[], Task> handler, ILogger logger)
        {
            _handler = handler;
            _logger = logger;
            _serviceBusProcessor.ProcessMessageAsync += OnMessageReceivedAsync;
            _serviceBusProcessor.ProcessErrorAsync += OnErrorAsync;
        }

        private async Task OnMessageReceivedAsync(ProcessMessageEventArgs args)
        {
            _logger!.LogDebug("Processing message for: {topic}/{subscription}", TopicName, Subscription);
            var body = args.Message.Body.ToArray();
            await _handler!.Invoke(body);
            await args.CompleteMessageAsync(args.Message);
        }

        private Task OnErrorAsync(ProcessErrorEventArgs args)
        {
            _logger!.LogError(args.Exception, "Error processing event - {ex}", args.Exception.ToString());
            return Task.CompletedTask;
        }
    }
}
