# MiniTransit

MiniTransit is a lightweight, extensible .NET messaging library designed as a simpler alternative to MassTransit and NServiceBus. It provides a minimal, easy-to-understand API for message-based communication in distributed systems, with a focus on flexibility and customizability.

---

## Features

- **Simple Core API** – Minimal abstractions for easy onboarding and maintenance.
- **Pluggable Transports** – Built-in support for In-Memory, RabbitMQ, and Azure Service Bus. Add your own by implementing `IMessageBus`.
- **Automatic Consumer Registration** – Register consumers via the builder for automatic subscription and background processing.
- **Configurable Retry Policies** – Built-in support for various retry strategies.
- **Custom Serialization** – Swap out the default JSON serializer for your own.
- **.NET Standard** – Works with modern .NET applications and libraries.

---

## Getting Started

### Installation

Add the core package and any transport you need:

```shell
dotnet add package MiniTransit
dotnet add package MiniTransit.RabbitMQ         # Optional: RabbitMQ support
dotnet add package MiniTransit.AzureServiceBus  # Optional: Azure Service Bus support
```

---

### Basic Usage

#### 1. Register MiniTransit and a Transport

Use the provided extension methods to register MiniTransit and select a transport. Configuration is done via a builder pattern:

```csharp
services.AddMiniTransit((settings, builder) =>
{
    builder.UseInMemory();
    // Or for RabbitMQ:
    // builder.UseRabbitMQ(options => { options.HostName = "localhost"; ... });
    // Or for Azure Service Bus:
    // builder.UseAzureServiceBus(options => { options.ConnectionString = "..."; ... });
});
```

#### 2. Register Consumers Automatically

You can register consumers for automatic background subscription and processing:

```csharp
services.AddMiniTransit((settings, builder) =>
{
    builder.UseInMemory();
    builder.AddConsumer<MyConsumer>();
    builder.AddConsumer<AnotherConsumer>();
});
```

When using `AddConsumer<T>()`, MiniTransit will automatically subscribe these consumers to the appropriate message types and run them as hosted services.

All consumers from an assembly can be registered automatically as well:

```csharp
services.AddMiniTransit((settings, builder) =>
{
    builder.UseInMemory();
    builder.AddConsumers(typeof(MyConsumer).Assembly);
});
```

#### 3. Define a Consumer

```csharp
public class MyConsumer : IConsumer<MyMessage>
{
    public Task ConsumeAsync(ConsumeContext<MyMessage> context)
    {
        // Handle the message
        return Task.CompletedTask;
    }
}
```

#### 4. Publish and Subscribe (Manual Subscription)

If you prefer manual subscription, you can still resolve `IBus` and subscribe:

```csharp
var bus = serviceProvider.GetRequiredService<IBus>();
await bus.SubscribeAsync<MyMessage, MyConsumer>();
await bus.StartProcessingAsync();

await bus.PublishAsync(new MyMessage { ... });
```

#### 5. Scheduling (if supported by transport)

```csharp
await bus.ScheduleAsync(new MyMessage { ... }, TimeSpan.FromSeconds(10));
```

---

## Retry Policy Configuration

MiniTransit supports flexible retry policies for message processing failures. Configure retry policies using the builder’s `UseRetry` method:

```csharp
services.AddMiniTransit((settings, builder) =>
{
    builder.UseInMemory();
    builder.UseRetry(retry =>
    {
        retry.Immediate(3); // 3 immediate retries
        retry.Ignore<ArgumentException>(); // Ignore specific exceptions
    });
});
```

Supported retry strategies include:

- **Immediate:** Retry immediately a specified number of times.
- **Interval:** Retry with a fixed interval.
- **Custom Intervals:** Retry with custom intervals.
- **Exponential Backoff:** Retry with exponentially increasing intervals.
- **Custom Policy:** Use your own implementation.

---

## Custom Serialization

You can replace the default JSON serializer with your own implementation:

```csharp
builder.UseMessageSerializer<MyCustomSerializer>();
```

---

## Transports

The following transports are currently supported:

### In-Memory Transport

An in-memory implementation using .NET Channels for asynchronous messaging and scheduling.

### RabbitMQ Transport

The RabbitMQ transport in MiniTransit provides robust messaging capabilities using RabbitMQ's topic exchanges and queues. It supports advanced features for message processing, retry policies, and scheduling.

#### Features

- **Durable Queues**: Option to create queues as durable for persistence across RabbitMQ restarts.
- **Dead Letter Queues**: Automatically route failed messages to a Dead Letter Queue for further inspection.
- **Retry Policies**: Configurable retry mechanism with support for retry counts and delayed retries.
- **Message Scheduling**: Leverages the `rabbitmq_delayed_message_exchange` plugin for delayed message delivery.
- **TLS Support**: Secure communication with RabbitMQ using TLS.
- **Prefetch Count**: Control the number of messages fetched from RabbitMQ in one go.
- **Auto Acknowledgment**: Automatically acknowledge messages upon receipt or process them manually.
- **Customizable Time-to-Live**: Set a default expiration time for messages.

#### How It Works

1. **Message Routing**: Messages are routed using RabbitMQ's topic exchanges. The routing key is derived from the message type name, ensuring that messages are delivered to all subscribed consumers.
2. **Queue Naming**: Unique queue names are generated based on the message type and consumer type, ensuring isolation between different consumers.
3. **Error Handling**: Failed messages are retried based on the configured retry policy. If retries are exhausted, messages can be sent to a Dead Letter Queue (if enabled).
4. **Scheduling**: Messages can be scheduled for future delivery using the RabbitMQ delayed message exchange plugin.

#### Configuration Example

```csharp
services.AddMiniTransit((settings, builder) =>
{
    builder.UseRabbitMQ(options =>
    {
        options.HostName = "localhost";
        options.Port = 5672;
        options.UserName = "guest";
        options.Password = "guest";
        options.DeadLetterOnError = true;
        options.DefaultMessageTimeToLiveDays = 365;
        // ...other settings
    });
});
```

### Azure Service Bus Transport

The Azure Service Bus transport in MiniTransit provides robust messaging capabilities using Azure's fully managed message broker. It supports advanced features for message processing, retry policies, and subscription management.

#### Features

- **Automatic Subscription Creation**: Automatically create topics and subscriptions if they do not exist.
- **Dead Letter Queues**: Route failed messages to a Dead Letter Queue for further inspection.
- **Retry Policies**: Configurable retry mechanism with support for retry counts and delayed retries.
- **Message Scheduling**: Schedule messages for future delivery.
- **Subject Filtering**: Filter messages to subscriptions based on the message type.
- **Lock Management**: Configurable lock duration and automatic lock renewal.
- **Concurrent Processing**: Control the number of concurrent message handlers.

#### How It Works

1. **Message Routing**: Messages are routed to topics and subscriptions. Subject filtering can be enabled to route messages based on their type.
2. **Subscription Management**: Subscriptions can be created automatically if they do not exist, with configurable settings such as message TTL, lock duration, and max delivery count. The provided connection string must have the 'Manage' claim to allow subscriptions to be managed dynamically.
3. **Error Handling**: Failed messages are retried based on the configured retry policy. If retries are exhausted, messages can be sent to a Dead Letter Queue (if enabled).
4. **Scheduling**: Messages can be scheduled for future delivery using Azure Service Bus's built-in scheduling capabilities.

#### Configuration Example

```csharp
services.AddMiniTransit((settings, builder) =>
{
    builder.UseAzureServiceBus(options =>
    {
        options.ConnectionString = "<your-connection-string>";
        options.CreateSubscriptions = true;
        options.MaxDeliveryCount = 3;
        // ...other settings
    });
});
```

### Transport Settings

Each transport has its own settings class for advanced configuration. See the `Settings` folder in each transport project for details.

---

## Extending MiniTransit

To add a new transport, implement the `IMessageBus` interface and provide an extension method for registration. See the `MiniTransit.RabbitMQ` and `MiniTransit.AzureServiceBus` projects for examples.

---

## Contributing

Contributions are welcome! Please open issues or submit pull requests.

---

## License

MIT
