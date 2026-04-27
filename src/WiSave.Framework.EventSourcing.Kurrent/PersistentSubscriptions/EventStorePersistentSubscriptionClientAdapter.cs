using System.Threading.Channels;
using EventStore.Client;
using Grpc.Core;

namespace WiSave.Framework.EventSourcing.Kurrent.PersistentSubscriptions;

public sealed class EventStorePersistentSubscriptionClientAdapter(
    EventStorePersistentSubscriptionsClient client) : IKurrentPersistentSubscriptionClient
{
    public async Task CreateToAllAsync(string groupName, KurrentPersistentSubscriptionCreateOptions options, CancellationToken ct)
    {
        var settings = new PersistentSubscriptionSettings(
            resolveLinkTos: false,
            startFrom: options.FromStartWhenCreated ? Position.Start : Position.End,
            maxSubscriberCount: options.MaxSubscriberCount,
            consumerStrategyName: options.ConsumerStrategyName);

        try
        {
            await client.CreateToAllAsync(groupName, settings, cancellationToken: ct);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.AlreadyExists)
        {
            throw new KurrentPersistentSubscriptionAlreadyExistsException(groupName, ex);
        }
    }

    public async Task<IKurrentPersistentSubscription> SubscribeToAllAsync(string groupName, CancellationToken ct)
    {
        var channel = Channel.CreateUnbounded<KurrentPersistentSubscriptionMessage>();

        var subscription = await client.SubscribeToAllAsync(
            groupName,
            async (sub, resolvedEvent, _, callbackCt) =>
            {
                await channel.Writer.WriteAsync(
                    new KurrentPersistentSubscriptionMessage.Event(
                        new KurrentCommittedEvent(
                            resolvedEvent.Event.EventId.ToGuid(),
                            resolvedEvent.Event.EventType,
                            resolvedEvent.OriginalStreamId,
                            resolvedEvent.Event.Data.ToArray(),
                            new EventStoreSubscriptionActions(sub, resolvedEvent))),
                    callbackCt);
            },
            (sub, reason, ex) =>
            {
                channel.Writer.TryComplete(
                    new KurrentPersistentSubscriptionDroppedException(
                        groupName,
                        reason.ToString(),
                        ex));
            },
            cancellationToken: ct);

        channel.Writer.TryWrite(new KurrentPersistentSubscriptionMessage.Confirmation(subscription.SubscriptionId));

        return new EventStorePersistentSubscriptionAdapter(subscription, channel.Reader);
    }

    private sealed class EventStorePersistentSubscriptionAdapter(
        PersistentSubscription subscription,
        ChannelReader<KurrentPersistentSubscriptionMessage> messages) : IKurrentPersistentSubscription
    {
        public IAsyncEnumerable<KurrentPersistentSubscriptionMessage> Messages => messages.ReadAllAsync();

        public ValueTask DisposeAsync()
        {
            subscription.Dispose();
            return ValueTask.CompletedTask;
        }
    }

    private sealed class EventStoreSubscriptionActions(
        PersistentSubscription subscription,
        ResolvedEvent resolvedEvent) : IKurrentSubscriptionActions
    {
        public Task AckAsync(CancellationToken ct) => subscription.Ack([resolvedEvent]);

        public Task RetryAsync(string reason, CancellationToken ct) =>
            subscription.Nack(PersistentSubscriptionNakEventAction.Retry, reason, [resolvedEvent]);

        public Task ParkAsync(string reason, CancellationToken ct) =>
            subscription.Nack(PersistentSubscriptionNakEventAction.Park, reason, [resolvedEvent]);

        public Task SkipAsync(string reason, CancellationToken ct) =>
            subscription.Nack(PersistentSubscriptionNakEventAction.Skip, reason, [resolvedEvent]);
    }
}
