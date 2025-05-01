
namespace MiniTransit.Policies
{
    public sealed class DefaultRetryPolicy : IRetryPolicy
    {
        public readonly TimeSpan[] RetryIntervals;
        public readonly HashSet<Type> IgnoreExceptions;

        public DefaultRetryPolicy(TimeSpan[] retryIntervals,
            HashSet<Type> ignoreExceptions)
        {
            RetryIntervals = retryIntervals;
            IgnoreExceptions = ignoreExceptions;
        }

        public bool ShouldRetry(int retryCount, Exception exception)
        {
            var exceptionType = exception.GetType();
            if (IgnoreExceptions.Contains(exceptionType))
            {
                return false;
            }

            return retryCount <= RetryIntervals.Length;
        }

        public bool ShouldThrow(Exception exception)
        {
            var exceptionType = exception.GetType();
            return !IgnoreExceptions.Contains(exceptionType);
        }

        public TimeSpan GetRetryDelay(int retryCount)
        {
            var intervalIndex = retryCount - 1;
            return RetryIntervals[intervalIndex];
        }
    }
}
