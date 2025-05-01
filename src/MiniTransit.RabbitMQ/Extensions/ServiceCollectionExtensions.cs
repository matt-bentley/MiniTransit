using MiniTransit;
using MiniTransit.Builders;
using MiniTransit.RabbitMQ.Factories;
using MiniTransit.RabbitMQ;
using MiniTransit.RabbitMQ.Settings;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Configures MiniTransit to use a RabbitMQ message bus.
        /// </summary>
        /// <param name="builder">The <see cref="IMiniTransitBuilder"/> to configure.</param>
        /// <param name="configure">The RabbitMQ settings to be configured.</param>
        /// <returns>The updated <see cref="IMiniTransitBuilder"/>.</returns>
        public static IMiniTransitBuilder UseRabbitMQ(this IMiniTransitBuilder builder, Action<RabbitMQMessageBusSettings> configureSettings)
        {
            var settings = RabbitMQMessageBusSettings.Default;
            configureSettings.Invoke(settings);
            builder.Services.AddSingleton(Options.Options.Create(settings));

            builder.Services.AddSingleton<RabbitMQMessageProcessorFactory>();
            builder.Services.AddSingleton<IMessageBus, RabbitMQMessageBus>();
            builder.Services.AddSingleton<RabbitMQConnectionFactory>();
            return builder;
        }
    }
}
