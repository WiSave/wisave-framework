using System.Text.Json;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WiSave.Framework.EventSourcing.Kurrent.Configuration;
using WiSave.Framework.EventSourcing.Kurrent.PersistentSubscriptions;

namespace WiSave.Framework.EventSourcing.Kurrent.Forwarding;

public sealed class KurrentToMassTransitForwarder(
    IKurrentPersistentSubscriptionClient client,
    KurrentSubscriptionBootstrapper bootstrapper,
    IServiceScopeFactory scopeFactory,
    IEventTypeRegistry eventTypeRegistry,
    IOptions<KurrentForwarderOptions> options,
    ILogger<KurrentToMassTransitForwarder> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var delay = TimeSpan.FromSeconds(Math.Max(1, options.Value.ReconnectDelaySeconds));
        var maxDelay = TimeSpan.FromSeconds(Math.Max(options.Value.ReconnectDelaySeconds, options.Value.MaxReconnectDelaySeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await bootstrapper.EnsureCreatedAsync(stoppingToken);
                break;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "KurrentDB not available yet. Retrying subscription bootstrap in {DelaySeconds}s",
                    delay.TotalSeconds);

                await Task.Delay(delay, stoppingToken);
                delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, maxDelay.TotalSeconds));
            }
        }

        delay = TimeSpan.FromSeconds(Math.Max(1, options.Value.ReconnectDelaySeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunSubscriptionLoopAsync(stoppingToken);
                delay = TimeSpan.FromSeconds(Math.Max(1, options.Value.ReconnectDelaySeconds));
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (KurrentPersistentSubscriptionDroppedException ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(
                    ex,
                    "Kurrent persistent subscription {GroupName} dropped with reason {DropReason}. Reconnecting in {ReconnectDelaySeconds}s",
                    ex.GroupName,
                    ex.Reason,
                    delay.TotalSeconds);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(
                    ex,
                    "Kurrent forwarder for group {GroupName} failed. Reconnecting in {ReconnectDelaySeconds}s",
                    options.Value.GroupName,
                    delay.TotalSeconds);
            }

            await Task.Delay(delay, stoppingToken);
            delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, maxDelay.TotalSeconds));
        }
    }

    private async Task RunSubscriptionLoopAsync(CancellationToken stoppingToken)
    {
        await using var subscription = await client.SubscribeToAllAsync(options.Value.GroupName, stoppingToken);

        await foreach (var message in subscription.Messages.WithCancellation(stoppingToken))
        {
            switch (message)
            {
                case KurrentPersistentSubscriptionMessage.Confirmation(var subscriptionId):
                    logger.LogInformation("Kurrent persistent subscription {SubscriptionId} connected", subscriptionId);
                    break;

                case KurrentPersistentSubscriptionMessage.Event(var committedEvent):
                    await HandleEventAsync(committedEvent, stoppingToken);
                    break;
            }
        }
    }

    public async Task<bool> HandleEventAsync(KurrentCommittedEvent committedEvent, CancellationToken ct)
    {
        if (options.Value.StreamPrefixes.Length == 0
            || !options.Value.StreamPrefixes.Any(prefix =>
                committedEvent.StreamId.StartsWith(prefix, StringComparison.Ordinal)))
        {
            await committedEvent.Actions.SkipAsync("Event stream is outside forwarder scope.", ct);
            return false;
        }

        var clrType = eventTypeRegistry.Resolve(committedEvent.EventType);
        if (clrType is null)
        {
            logger.LogWarning("Parking unknown committed event type {EventType} from stream {StreamId}", committedEvent.EventType, committedEvent.StreamId);
            await committedEvent.Actions.ParkAsync($"Unknown event type {committedEvent.EventType}.", ct);
            return false;
        }

        try
        {
            var message = JsonSerializer.Deserialize(committedEvent.Data, clrType, JsonOptions);
            if (message is null)
            {
                logger.LogWarning("Parking committed event {EventType} because payload deserialized to null", committedEvent.EventType);
                await committedEvent.Actions.ParkAsync($"Payload for {committedEvent.EventType} deserialized to null.", ct);
                return false;
            }

            await using var scope = scopeFactory.CreateAsyncScope();
            var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

            await publishEndpoint.Publish(message, publishContext =>
            {
                publishContext.MessageId = committedEvent.EventId;
            }, ct);

            await committedEvent.Actions.AckAsync(ct);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to forward committed event {EventType} from stream {StreamId}", committedEvent.EventType, committedEvent.StreamId);
            await committedEvent.Actions.RetryAsync(ex.Message, ct);
            return false;
        }
    }
}
