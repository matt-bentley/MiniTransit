using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace MiniTransit
{
    public sealed class InMemoryMessageBus : IMessageBus
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, Channel<byte[]>>> _topics = new();
        private readonly ConcurrentDictionary<string, Task> _subscriberTasks = new();
        private readonly PriorityQueue<ScheduledMessage, DateTime> _scheduleQueue = new();
        private readonly object _scheduleQueueLock = new();
        private readonly SemaphoreSlim _signal = new(0);
        private readonly CancellationTokenSource _cts = new();
        private Task? _schedulerTask;
        private readonly ILogger<InMemoryMessageBus> _logger;
        private int _isStarted = 0;
        private int _isStopped = 0;
        private bool _disposed = false;
        private readonly List<Subscription> _pendingSubscribers = new();
        private readonly object _pendingLock = new();

        public InMemoryMessageBus(ILogger<InMemoryMessageBus> logger)
        {
            _logger = logger;
        }

        public async Task PublishAsync(byte[] message, string topic, string subject)
        {
            var topicKey = GetChannelKey(topic, subject);
            if(_topics.TryGetValue(topicKey, out var subscriptions))
            {
                foreach(var subscription in subscriptions)
                {
                    await subscription.Value.Writer.WriteAsync(message);
                }
            }
        }

        public Task ScheduleAsync(byte[] message, string topic, string subject, TimeSpan delay)
        {
            var scheduledTime = DateTime.UtcNow.Add(delay);

            lock (_scheduleQueueLock)
            {
                _scheduleQueue.Enqueue(new ScheduledMessage(message, topic, subject), scheduledTime);
            }

            // Signal the scheduler loop there's a new scheduled item  
            _signal.Release();
 
            return Task.CompletedTask;
        }

        public Task SubscribeAsync(string topic, string subject, string subscriptionName, Func<byte[], Task> handler)
        {
            if (_isStarted == 0)
            {
                lock (_pendingLock)
                {
                    _pendingSubscribers.Add(new Subscription(topic, subject, subscriptionName, handler));
                }
            }
            else
            {
                StartSubscriber(new Subscription(topic, subject, subscriptionName, handler));
            }
            return Task.CompletedTask;
        }

        private void StartSubscriber(Subscription subscription)
        {
            var topicKey = GetChannelKey(subscription.Topic, subscription.Subject);
            var subscriptions = _topics.GetOrAdd(topicKey, _ => new ConcurrentDictionary<string, Channel<byte[]>>());
            var subscriberKey = $"{subscription.Topic}.{subscription.Subject}.{subscription.SubscriptionName}";
            var topicSubscription = subscriptions.GetOrAdd(subscription.SubscriptionName, _ => Channel.CreateUnbounded<byte[]>());

            var subscriberTask = Task.Run(async () =>
            {
                while (!_cts.IsCancellationRequested)
                {
                    try
                    {
                        await foreach (var message in topicSubscription.Reader.ReadAllAsync(_cts.Token))
                        {
                            try
                            {
                                await subscription.Handler.Invoke(message);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error processing message: {message}", ex.Message);
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Subscriber {subscriberKey} encountered an unexpected error. Restarting subscription loop.", subscriberKey);
                        try { await Task.Delay(TimeSpan.FromSeconds(1), _cts.Token); } catch { break; }
                    }
                }
            }, _cts.Token);

            _subscriberTasks[subscriberKey] = subscriberTask;
        }

        private static string GetChannelKey(string topic, string subject) => $"{topic}.{subject}";

        public async Task StartProcessingAsync()
        {
            if (Interlocked.Exchange(ref _isStarted, 1) == 0)
            {
                List<Subscription> toStart;
                lock (_pendingLock)
                {
                    toStart = _pendingSubscribers.ToList();
                    _pendingSubscribers.Clear();
                }
                foreach (var subscription in toStart)
                {
                    StartSubscriber(subscription);
                }
                _schedulerTask = Task.Run(SchedulerLoopAsync);
            }
            await Task.CompletedTask;
        }

        public async Task StopProcessingAsync()
        {
            if (Interlocked.Exchange(ref _isStopped, 1) == 0)
            {
                _cts.Cancel();

                _signal.Release();

                // Wait for scheduler and all subscribers to finish  
                var tasks = new List<Task>();
                if (_schedulerTask != null) tasks.Add(_schedulerTask);
                tasks.AddRange(_subscriberTasks.Values);

                try
                {
                    await Task.WhenAll(tasks);
                }
                catch (OperationCanceledException)
                {
                    // Expected on shutdown  
                }
                catch (AggregateException aex) when (aex.InnerExceptions.All(e => e is OperationCanceledException))
                {
                    // Also expected  
                }
            }
        }

        private async Task SchedulerLoopAsync()
        {
            while (!_cts.IsCancellationRequested)
            {
                ScheduledMessage? nextMsg;
                DateTime nextScheduledTime;

                lock (_scheduleQueueLock)
                {
                    if(!_scheduleQueue.TryPeek(out nextMsg, out nextScheduledTime))
                    {
                        // No scheduled messages, wait for a signal  
                        nextScheduledTime = DateTime.UtcNow + TimeSpan.FromMilliseconds(int.MaxValue);
                    }
                }

                var delay = nextScheduledTime - DateTime.UtcNow;

                if (delay <= TimeSpan.Zero)
                {
                    // Message is due or overdue, dequeue now  
                    lock (_scheduleQueueLock)
                    {
                        if(_scheduleQueue.TryPeek(out nextMsg, out nextScheduledTime) && nextScheduledTime <= DateTime.UtcNow)
                        {
                            nextMsg = _scheduleQueue.Dequeue();
                        }
                        else
                        {
                            continue; // another thread may have handled it  
                        }
                    }

                    await PublishAsync(nextMsg.Message, nextMsg.Topic, nextMsg.Subject);
                }
                else
                {
                    // Wait efficiently until the next message scheduled time, or a new scheduled message signal occurs  
                    await _signal.WaitAsync(delay, _cts.Token);
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            await StopProcessingAsync();
            _cts.Dispose();
            _signal.Dispose();
        }

        private sealed record ScheduledMessage(byte[] Message, string Topic, string Subject);
        private sealed record Subscription(string Topic, string Subject, string SubscriptionName, Func<byte[], Task> Handler);
    }
}