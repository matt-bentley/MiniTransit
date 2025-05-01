
namespace MiniTransit.AzureServiceBus.Settings
{
    public class AzureServiceBusMessageBusSettings
    {
        /// <summary>
        /// The policy for this connection string must have the 'Manage' claim on the service bus if CreateSubscriptions=True.
        /// Otherwise the policy must have the 'Send' and 'Listen' claims on the service bus.
        /// </summary>
        public string? ConnectionString { get; set; }

        /// <summary>
        /// Create the processor subscription if it does not exist
        /// </summary>
        public bool CreateSubscriptions { get; set; }

        /// <summary>
        /// Maximum attempts to process message if there is a failure. 
        /// Only applicable if CreateSubscriptions=True.
        /// </summary>
        public int MaxDeliveryCount { get; set; }

        /// <summary>
        /// Default time for messages to live without being processed before exiring. 
        /// Only applicable if CreateSubscriptions=True.
        /// </summary>
        public int DefaultMessageTimeToLiveDays { get; set; }

        /// <summary>
        /// Dead letter message on expiration. 
        /// Only applicable if CreateSubscriptions=True.
        /// </summary>
        public bool DeadLetteringOnMessageExpiration { get; set; }

        /// <summary>
        /// Duration of a peek lock receive.i.e., the amount of time that the message is locked by a given receiver so that no other receiver receives the same message.
        /// Max value is 5 minutes. Default value is 60 seconds.
        /// Only applicable if CreateSubscriptions=True.
        /// </summary>
        public int LockDurationSeconds { get; set; }

        /// <summary>
        /// Gets or sets the maximum duration within which the lock will be renewed automatically. This
        /// value should be greater than the longest message lock duration; for example, the LockDurationSeconds Property.
        /// </summary>
        ///
        /// <value>The maximum duration during which message locks are automatically renewed. The default value is 5 minutes.</value>
        public int MaxAutoLockRenewalDurationSeconds { get; set; }

        /// <summary>
        /// Filter messages to a subscription based on the message Subject.
        /// This allows a single topic to be used to handle different types of messages for each subscription.
        /// Only applicable if CreateSubscriptions=True.
        /// </summary>
        public bool SubjectFiltering { get; set; }

        /// <summary>
        /// The maximum number of concurrent calls to the message handler. The default value is 1.
        /// </summary>
        public int MaxConcurrentCalls { get; set; }

        public static AzureServiceBusMessageBusSettings Default => new()
        {
            CreateSubscriptions = true,
            DeadLetteringOnMessageExpiration = true,
            DefaultMessageTimeToLiveDays = 365,
            LockDurationSeconds = 60,
            MaxAutoLockRenewalDurationSeconds = 60 * 5,
            MaxConcurrentCalls = 1,
            MaxDeliveryCount = 3,
            SubjectFiltering = true
        };
    }
}
