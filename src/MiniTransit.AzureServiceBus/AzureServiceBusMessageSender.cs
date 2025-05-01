using Azure.Messaging.ServiceBus;

namespace MiniTransit.AzureServiceBus
{
    public sealed class AzureServiceBusMessageSender
    {
        private readonly ServiceBusSender _sender;

        public AzureServiceBusMessageSender(ServiceBusSender sender)
        {
            _sender = sender;
        }

        public async Task SendAsync(byte[] message, string subject)
        {
            await _sender.SendMessageAsync(CreateServiceBusMessage(message, subject));
        }

        public async Task ScheduleAsync(byte[] message, string subject, DateTime scheduledEnqueueTime)
        {
            await _sender.ScheduleMessageAsync(CreateServiceBusMessage(message, subject), scheduledEnqueueTime);
        }

        private ServiceBusMessage CreateServiceBusMessage(byte[] message, string subject)
        {
            return new ServiceBusMessage(message)
            {
                MessageId = Guid.NewGuid().ToString(),
                Subject = subject
            };
        }
    }
}
