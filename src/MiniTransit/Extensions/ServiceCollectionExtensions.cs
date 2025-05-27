using Microsoft.Extensions.DependencyInjection.Extensions;
using MiniTransit;
using MiniTransit.Builders;
using MiniTransit.Serialization;
using MiniTransit.Settings;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds MiniTransit services to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The service collection to add the services to.</param>
        /// <param name="configure">A delegate to configure <see cref="MiniTransitSettings"/> and <see cref="IMiniTransitBuilder"/>.</param>
        /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddMiniTransit(this IServiceCollection services, Action<MiniTransitSettings, IMiniTransitBuilder> configure)
        {
            var settings = new MiniTransitSettings()
            {
                PublishTopic = "events",
                SubscribeTopic = "events"
            };
            var builder = new MiniTransitBuilder(services);
            services.AddSingleton<IBus, MiniTransitBus>();
            services.AddSingleton<IMessageSerializer, JsonMessageSerializer>();
            configure.Invoke(settings, builder);
            services.AddSingleton(Options.Options.Create(settings));
            return services;
        }

        /// <summary>
        /// Configures MiniTransit to use an in-memory message bus.
        /// </summary>
        /// <param name="builder">The <see cref="IMiniTransitBuilder"/> to configure.</param>
        /// <returns>The updated <see cref="IMiniTransitBuilder"/>.</returns>
        public static IMiniTransitBuilder UseInMemory(this IMiniTransitBuilder builder)
        {
            builder.Services.AddSingleton<IMessageBus, InMemoryMessageBus>();
            return builder;
        }

        /// <summary>
        /// Adds all consumers from the calling assembly to the MiniTransit configuration.
        /// </summary>
        /// <param name="builder">The <see cref="IMiniTransitBuilder"/> to configure.</param>
        /// <returns>The updated <see cref="IMiniTransitBuilder"/>.</returns>
        public static IMiniTransitBuilder AddConsumers(this IMiniTransitBuilder builder)
        {
            var callingAssembly = Assembly.GetCallingAssembly();
            return builder.AddConsumers(callingAssembly);
        }

        /// <summary>
        /// Adds all consumers from the specified assembly to the MiniTransit configuration.
        /// </summary>
        /// <param name="builder">The <see cref="IMiniTransitBuilder"/> to configure.</param>
        /// <param name="assembly">The assembly to scan for consumers.</param>
        /// <returns>The updated <see cref="IMiniTransitBuilder"/>.</returns>
        public static IMiniTransitBuilder AddConsumers(this IMiniTransitBuilder builder, Assembly assembly)
        {
            var consumerTypes = assembly.GetTypes()
                .Where(type => !type.IsAbstract &&
                    type.GetInterfaces()
                        .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsumer<>)));

            foreach (var consumerType in consumerTypes)
            {
                builder.AddConsumer(consumerType);
            }

            return builder;
        }

        /// <summary>
        /// Adds a consumer to the MiniTransit configuration and subscribes to messages. An IHostedService is added to consume messages.
        /// </summary>
        /// <typeparam name="TConsumer">The type of the consumer to add. Must implement <see cref="IConsumer{TMessage}"/>.</typeparam>
        /// <param name="builder">The <see cref="IMiniTransitBuilder"/> to configure.</param>
        /// <returns>The updated <see cref="IMiniTransitBuilder"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if <typeparamref name="TConsumer"/> does not implement <see cref="IConsumer{TMessage}"/>.</exception>
        public static IMiniTransitBuilder AddConsumer<TConsumer>(this IMiniTransitBuilder builder)
            where TConsumer : class, IConsumer
        {
            return builder.AddConsumer(typeof(TConsumer));
        }

        /// <summary>
        /// Adds a consumer to the MiniTransit configuration and subscribes to messages. An IHostedService is added to consume messages.
        /// </summary>
        /// <param name="builder">The <see cref="IMiniTransitBuilder"/> to configure.</param>
        /// <param name="consumerType">The type of the consumer to add.  Must implement <see cref="IConsumer{TMessage}"/>.</param>
        /// <returns>The updated <see cref="IMiniTransitBuilder"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if <typeparamref name="TConsumer"/> does not implement <see cref="IConsumer{TMessage}"/>.</exception>
        public static IMiniTransitBuilder AddConsumer(this IMiniTransitBuilder builder, Type consumerType)
        {
            var consumerInterface = consumerType.GetInterface("IConsumer`1");
            if (consumerInterface == null)
            {
                throw new InvalidOperationException($"{consumerType.Name} must implement IConsumer<TMessage>.");
            }

            var messageType = consumerInterface.GetGenericArguments()[0];

            builder.Services.TryAddTransient(consumerType);

            var addHostedServiceMethod = typeof(ServiceCollectionHostedServiceExtensions)
                .GetMethods()
                .First(m => m.Name == "AddHostedService" && m.IsGenericMethod);

            var genericMethod = addHostedServiceMethod.MakeGenericMethod(
                typeof(ConsumerHostedService<,>).MakeGenericType(messageType, consumerType)
            );

            genericMethod.Invoke(null, [builder.Services]);

            return builder;
        }

        /// <summary>
        /// Configures MiniTransit to use a custom message serializer.
        /// </summary>
        /// <typeparam name="T">The type of the custom message serializer. Must implement <see cref="IMessageSerializer"/>.</typeparam>
        /// <param name="builder">The <see cref="IMiniTransitBuilder"/> to configure.</param>
        /// <returns>The updated <see cref="IMiniTransitBuilder"/>.</returns>
        public static IMiniTransitBuilder UseMessageSerializer<T>(this IMiniTransitBuilder builder)
            where T : class, IMessageSerializer
        {
            builder.Services.RemoveAll<IMessageSerializer>();
            builder.Services.AddSingleton<IMessageSerializer, T>();
            return builder;
        }

        /// <summary>
        /// Configures MiniTransit to use a retry policy for message processing.
        /// </summary>
        /// <param name="builder">The <see cref="IMiniTransitBuilder"/> to configure.</param>
        /// <param name="configure">A delegate to configure the <see cref="RetryPolicyBuilder"/>.</param>
        /// <returns>The updated <see cref="IMiniTransitBuilder"/>.</returns>
        public static IMiniTransitBuilder UseRetry(this IMiniTransitBuilder builder, Action<RetryPolicyBuilder> configure)
        {
            var retryPolicyBuilder = new RetryPolicyBuilder();
            configure.Invoke(retryPolicyBuilder);
            retryPolicyBuilder.Register(builder.Services);
            return builder;
        }
    }
}
