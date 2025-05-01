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

## Transport Settings

Each transport has its own settings class for advanced configuration. See the `Settings` folder in each transport project for details.

### RabbitMQ Example

```csharp
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
```

### Azure Service Bus Example

```csharp
builder.UseAzureServiceBus(options =>
{
    options.ConnectionString = "<your-connection-string>";
    options.CreateSubscriptions = true;
    options.MaxDeliveryCount = 3;
    // ...other settings
});
```

---

## Extending MiniTransit

To add a new transport, implement the `IMessageBus` interface and provide an extension method for registration. See the `MiniTransit.RabbitMQ` and `MiniTransit.AzureServiceBus` projects for examples.

---

## Contributing

Contributions are welcome! Please open issues or submit pull requests.

---

## License

MIT
