using System.Text;
using System.Text.Json;
using EventStore.Client;
using WiSave.Framework.Application;
using WiSave.Framework.Domain;

namespace WiSave.Framework.EventSourcing.Kurrent;

public sealed class KurrentDbAggregateRepository<TAggregate, TId>(
    EventStoreClient client,
    IEventTypeRegistry eventTypeRegistry,
    IEventTypeNameResolver eventTypeNameResolver) : IAggregateRepository<TAggregate, TId>
    where TAggregate : AggregateRoot<TId>, IAggregateStream<TId>, new()
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
    };

    public async Task<TAggregate?> LoadAsync(TId id, CancellationToken ct = default)
    {
        var streamId = TAggregate.ToStreamId(id);
        var aggregate = new TAggregate();
        var events = new List<object>();

        try
        {
            var result = client.ReadStreamAsync(Direction.Forwards, streamId, StreamPosition.Start, cancellationToken: ct);

            await foreach (var resolved in result)
            {
                var eventType = eventTypeRegistry.Resolve(resolved.Event.EventType);
                if (eventType is null) continue;

                var data = Encoding.UTF8.GetString(resolved.Event.Data.Span);
                var @event = JsonSerializer.Deserialize(data, eventType, JsonOptions);
                if (@event is not null)
                    events.Add(@event);
            }
        }
        catch (StreamNotFoundException)
        {
            return null;
        }

        if (events.Count == 0)
            return null;

        aggregate.ReplayEvents(events);
        return aggregate;
    }

    public async Task SaveAsync(TAggregate aggregate, CancellationToken ct = default)
    {
        var uncommitted = aggregate.GetUncommittedEvents();
        if (uncommitted.Count == 0) return;

        var streamId = TAggregate.ToStreamId(aggregate.Id);
        var expectedRevision = aggregate.Version < 0
            ? StreamRevision.None
            : StreamRevision.FromInt64(aggregate.Version);

        var eventData = uncommitted.Select(e =>
        {
            var typeName = eventTypeNameResolver.Resolve(e.GetType());
            var json = JsonSerializer.SerializeToUtf8Bytes(e, e.GetType(), JsonOptions);
            return new EventData(Uuid.NewUuid(), typeName, json);
        }).ToArray();

        if (aggregate.Version < 0)
        {
            await client.AppendToStreamAsync(streamId, StreamState.NoStream, eventData, cancellationToken: ct);
        }
        else
        {
            await client.AppendToStreamAsync(streamId, expectedRevision, eventData, cancellationToken: ct);
        }

        aggregate.ClearUncommittedEvents();
    }
}
