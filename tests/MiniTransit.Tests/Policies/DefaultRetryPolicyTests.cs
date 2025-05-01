using MiniTransit.Policies;

namespace MiniTransit.Tests.Policies
{
    public class DefaultRetryPolicyTests
    {
        [Fact]
        public void ShouldRetry_ReturnsFalse_WhenExceptionIsIgnored()
        {
            // Arrange
            var retryIntervals = new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2) };
            var ignoreExceptions = new HashSet<Type> { typeof(InvalidOperationException) };
            var policy = new DefaultRetryPolicy(retryIntervals, ignoreExceptions);
            var exception = new InvalidOperationException();

            // Act
            var result = policy.ShouldRetry(1, exception);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ShouldRetry_ReturnsTrue_WhenRetryCountIsWithinLimits()
        {
            // Arrange
            var retryIntervals = new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2) };
            var ignoreExceptions = new HashSet<Type>();
            var policy = new DefaultRetryPolicy(retryIntervals, ignoreExceptions);
            var exception = new Exception();

            // Act
            var result = policy.ShouldRetry(2, exception);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ShouldRetry_ReturnsFalse_WhenRetryCountExceedsLimits()
        {
            // Arrange
            var retryIntervals = new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2) };
            var ignoreExceptions = new HashSet<Type>();
            var policy = new DefaultRetryPolicy(retryIntervals, ignoreExceptions);
            var exception = new Exception();

            // Act
            var result = policy.ShouldRetry(3, exception);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetRetryDelay_ReturnsCorrectInterval_ForValidRetryCount()
        {
            // Arrange
            var retryIntervals = new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2) };
            var ignoreExceptions = new HashSet<Type>();
            var policy = new DefaultRetryPolicy(retryIntervals, ignoreExceptions);

            // Act
            var delay = policy.GetRetryDelay(2);

            // Assert
            Assert.Equal(TimeSpan.FromSeconds(2), delay);
        }

        [Fact]
        public void GetRetryDelay_ThrowsException_WhenRetryCountIsOutOfRange()
        {
            // Arrange
            var retryIntervals = new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2) };
            var ignoreExceptions = new HashSet<Type>();
            var policy = new DefaultRetryPolicy(retryIntervals, ignoreExceptions);

            // Act & Assert
            Assert.Throws<IndexOutOfRangeException>(() => policy.GetRetryDelay(3));
        }
    }
}
