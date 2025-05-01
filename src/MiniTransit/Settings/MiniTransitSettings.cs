
namespace MiniTransit.Settings
{
    public class MiniTransitSettings
    {
        /// <summary>
        /// The default topic to subscribe to
        /// </summary>
        public required string SubscribeTopic { get; set; }

        /// <summary>
        /// The default topic to publish to
        /// </summary>
        public required string PublishTopic { get; set; }
    }
}
