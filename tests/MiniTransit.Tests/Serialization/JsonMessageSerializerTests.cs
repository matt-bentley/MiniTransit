using MiniTransit.Serialization;
using MiniTransit.Subscriptions;
using System.Text;

namespace MiniTransit.Tests.Serialization
{
    public class JsonMessageSerializerTests
    {
        private readonly JsonMessageSerializer _serializer;

        public JsonMessageSerializerTests()
        {
            _serializer = new JsonMessageSerializer();
        }

        [Fact]
        public void Serialize_ShouldReturnValidJsonString()
        {
            // Arrange
            var message = new MessageEnvelope<string>
            {
                Message = "Test Message",
                SubscriptionContext = new SubscriptionContext("topic", "subscription", "messageType", "consumer", 0)
            };

            // Act
            var jsonBytes = _serializer.Serialize(message);
            var json = Encoding.UTF8.GetString(jsonBytes);

            // Assert
            Assert.NotNull(json);
            Assert.Contains("\"Message\":\"Test Message\"", json);
        }

        [Fact]
        public void Deserialize_ShouldReturnValidMessageEnvelope()
        {
            // Arrange
            var json = "{\"Message\":\"Test Message\",\"Metadata\":{\"Timestamp\":\"2025-04-16T00:00:00Z\"}}";

            // Act
            var jsonBytes = Encoding.UTF8.GetBytes(json);
            var result = _serializer.Deserialize<string>(jsonBytes);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Message", result.Message);
        }

        [Fact]
        public void Deserialize_InvalidJson_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var invalidJson = "null";
            var jsonBytes = Encoding.UTF8.GetBytes(invalidJson);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _serializer.Deserialize<string>(jsonBytes));
        }
    }
}
