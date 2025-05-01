using Microsoft.Extensions.DependencyInjection;
using MiniTransit.Policies;

namespace MiniTransit.Builders
{
    public sealed class RetryPolicyBuilder
    {
        private TimeSpan[]? _intervals;
        private HashSet<Type> _ignoreExceptions = new();
        private Type? _customPolicy;
        private ServiceLifetime _customPolicyLifetime;

        /// <summary>
        /// Configures the retry policy to retry immediately for a specified number of attempts.
        /// </summary>
        /// <param name="maxRetryCount">The maximum number of retry attempts.</param>
        /// <returns>The current <see cref="RetryPolicyBuilder"/> instance.</returns>
        public RetryPolicyBuilder Immediate(int maxRetryCount)
        {
            _intervals = Enumerable.Range(0, maxRetryCount)
                                   .Select(e => TimeSpan.Zero)
                                   .ToArray();
            return this;
        }

        /// <summary>
        /// Configures the retry policy to retry at fixed intervals for a specified number of attempts.
        /// </summary>
        /// <param name="maxRetryCount">The maximum number of retry attempts.</param>
        /// <param name="delay">The delay between each retry attempt.</param>
        /// <returns>The current <see cref="RetryPolicyBuilder"/> instance.</returns>
        public RetryPolicyBuilder Interval(int maxRetryCount, TimeSpan delay)
        {
            _intervals = Enumerable.Range(0, maxRetryCount)
                                   .Select(e => delay)
                                   .ToArray();
            return this;
        }

        /// <summary>
        /// Configures the retry policy with a custom sequence of retry intervals.
        /// </summary>
        /// <param name="intervals">An enumerable of <see cref="TimeSpan"/> values representing the retry intervals.</param>
        /// <returns>The current <see cref="RetryPolicyBuilder"/> instance.</returns>
        public RetryPolicyBuilder Intervals(IEnumerable<TimeSpan> intervals)
        {
            _intervals = intervals.ToArray();
            return this;
        }

        /// <summary>
        /// Configures the retry policy to use an exponential backoff strategy.
        /// </summary>
        /// <param name="maxRetryCount">The maximum number of retry attempts.</param>
        /// <param name="initialDelay">The initial delay before the first retry attempt.</param>
        /// <param name="multiplier">The multiplier to apply to the delay for each subsequent retry. Defaults to 2.0.</param>
        /// <returns>The current <see cref="RetryPolicyBuilder"/> instance.</returns>
        public RetryPolicyBuilder Exponential(int maxRetryCount, TimeSpan initialDelay, double multiplier = 2.0)
        {
            _intervals = Enumerable.Range(0, maxRetryCount)
                                   .Select(retry => TimeSpan.FromMilliseconds(initialDelay.TotalMilliseconds * Math.Pow(multiplier, retry)))
                                   .ToArray();
            return this;
        }

        /// <summary>
        /// Configures the retry policy to ignore specific exception types. Ignored exceptions will not trigger a retry.
        /// </summary>
        /// <typeparam name="T">The type of exception to ignore.</typeparam>
        /// <returns>The current <see cref="RetryPolicyBuilder"/> instance.</returns>
        public RetryPolicyBuilder Ignore<T>() where T : Exception
        {
            _ignoreExceptions.Add(typeof(T));
            return this;
        }

        /// <summary>
        /// Configures the retry policy to use a custom implementation of <see cref="IRetryPolicy"/>.
        /// </summary>
        /// <typeparam name="T">The type of the custom retry policy, which must implement <see cref="IRetryPolicy"/>.</typeparam>
        /// <param name="serviceLifetime">The lifetime of the custom retry policy in the dependency injection container. Defaults to <see cref="ServiceLifetime.Singleton"/>.</param>
        /// <returns>The current <see cref="RetryPolicyBuilder"/> instance.</returns>
        public RetryPolicyBuilder Custom<T>(ServiceLifetime serviceLifetime = ServiceLifetime.Singleton) where T : IRetryPolicy
        {
            _customPolicy = typeof(T);
            _customPolicyLifetime = serviceLifetime;
            return this;
        }

        internal IServiceCollection Register(IServiceCollection services, ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
        {
            if (_customPolicy != null)
            {
                var serviceDescriptor = new ServiceDescriptor(typeof(IRetryPolicy), _customPolicy, _customPolicyLifetime);
                services.Add(serviceDescriptor);
            }
            else if (_intervals != null)
            {
                services.AddSingleton<IRetryPolicy>(new DefaultRetryPolicy(_intervals, _ignoreExceptions));
            }

            return services;
        }
    }
}
