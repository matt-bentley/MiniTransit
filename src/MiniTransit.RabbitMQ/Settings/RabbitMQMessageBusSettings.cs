
namespace MiniTransit.RabbitMQ.Settings
{
    public class RabbitMQMessageBusSettings
    {
        public required string HostName { get; set; }
        public required string VirtualHost { get; set; }
        public required string UserName { get; set; }
        public required string Password { get; set; }
        public int Port { get; set; }

        /// <summary>
        /// Maximum attempts to process message if there is a failure.
        /// </summary>
        public int MaxDeliveryCount { get; set; }

        /// <summary>
        /// Default time for messages to live without being processed before exiring. Defaults to 365 days.
        /// </summary>
        public int DefaultMessageTimeToLiveDays { get; set; }

        /// <summary>
        /// Create a Dead Letter Queue for messages that are not processed successfully. This can not be used in conjunction with AutoAck. Defaults to true.
        /// </summary>
        public bool DeadLetterOnError { get; set; }

        /// <summary>
        /// RabbitMQ server has rabbitmq_delayed_message_exchange plugin 
        /// installed and enabled. Defaults to true.
        /// </summary>
        public bool SchedulingEnabled { get; set; }

        /// <summary>
        /// Create queues as durable. Defaults to false.
        /// </summary>
        public bool DurableQueues { get; set; }

        /// <summary>
        /// Enable TLS connection to RabbiqMQ. Defaults to false.
        /// </summary>
        public bool EnableTls { get; set; }

        /// <summary>
        /// Prefetch message count. This controls how many messages are pulled from RabbitMQ
        /// in one go. Defaults to 1.
        /// </summary>
        public ushort PrefetchCount { get; set; }

        /// <summary>
        /// Auto-Acknowledge messages when read from a queue. 
        /// This can not be used in conjunction with DeadLetterOnError.
        /// </summary>
        public bool AutoAck { get; set; }

        public static RabbitMQMessageBusSettings Default => new RabbitMQMessageBusSettings()
        {
            DeadLetterOnError = true,
            DefaultMessageTimeToLiveDays = 365,
            MaxDeliveryCount = 3,
            DurableQueues = false,
            HostName = "localhost",
            VirtualHost = "/",
            UserName = "guest",
            Password = "guest",
            Port = 5672,
            SchedulingEnabled = true,
            EnableTls = false,
            PrefetchCount = 1,
            AutoAck = false
        };
    }
}
