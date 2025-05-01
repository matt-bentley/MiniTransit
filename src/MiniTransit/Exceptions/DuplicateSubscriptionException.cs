
namespace MiniTransit.Exceptions
{
    public class DuplicateSubscriptionException : Exception
    {
        public DuplicateSubscriptionException(string subscription) : base($"Duplicate subscription: {subscription}")
        {

        }
    }
}
