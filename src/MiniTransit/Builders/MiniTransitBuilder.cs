using Microsoft.Extensions.DependencyInjection;

namespace MiniTransit.Builders
{
    public interface IMiniTransitBuilder
    {
        public IServiceCollection Services { get; }
    }

    internal class MiniTransitBuilder : IMiniTransitBuilder
    {
        public IServiceCollection Services { get; }

        public MiniTransitBuilder(IServiceCollection services)
        {
            Services = services;
        }
    }
}
