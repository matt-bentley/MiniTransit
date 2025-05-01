
namespace MiniTransit.Policies
{
    public interface IRetryPolicy
    {
        bool ShouldRetry(int retryCount, Exception exception);
        bool ShouldThrow(Exception exception);
        TimeSpan GetRetryDelay(int retryCount);
    }
}
