using MiniTransit.Policies;

namespace MiniTransit.Tests.Policies
{
    public class MockRetryPolicy : IRetryPolicy
    {
        public int MaxRetryCount { get; set; }

        public TimeSpan GetRetryDelay(int retryCount)
        {
            return TimeSpan.Zero;
        }

        public bool ShouldRetry(int retryCount, Exception exception)
        {
            return retryCount < MaxRetryCount;
        }

        public bool ShouldThrow(Exception exception)
        {
            return true;
        }
    }
}
