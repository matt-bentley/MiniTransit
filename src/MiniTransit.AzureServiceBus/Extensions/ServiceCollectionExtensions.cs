using MiniTransit;
using MiniTransit.Builders;
using MiniTransit.AzureServiceBus.Settings;
using MiniTransit.AzureServiceBus.Factories;
using MiniTransit.AzureServiceBus;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Configures MiniTransit to use an Azure Service Bus message bus.
        /// </summary>
        /// <param name="builder">The <see cref="IMiniTransitBuilder"/> to configure.</param>
        /// <param name="configure">The Azure Service Bus settings to be configured.</param>
        /// <returns>The updated <see cref="IMiniTransitBuilder"/>.</returns>
        public static IMiniTransitBuilder UseAzureServiceBus(this IMiniTransitBuilder builder, Action<AzureServiceBusMessageBusSettings> configureSettings)
        {
            var settings = AzureServiceBusMessageBusSettings.Default;
            configureSettings.Invoke(settings);
            builder.Services.AddSingleton(Options.Options.Create(settings));

            builder.Services.AddSingleton<AzureServiceBusMessageProcessorFactory>();
            builder.Services.AddSingleton<AzureServiceBusMessageSenderFactory>();
            builder.Services.AddSingleton<IMessageBus, AzureServiceBusMessageBus>();
            return builder;
        }
    }
}
