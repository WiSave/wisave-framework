using System.Collections.Concurrent;
using System.Reflection;

namespace WiSave.Framework.Domain;

public interface IAggregateStream<TId>
{
    static abstract string ToStreamId(TId id);
}

public abstract class AggregateRoot<TId>
{
    private static readonly ConcurrentDictionary<(Type Aggregate, Type Event), MethodInfo> MethodCache = new();

    public TId Id { get; protected set; } = default!;
    public int Version { get; private set; } = -1;

    private readonly List<object> _uncommittedEvents = [];

    public IReadOnlyList<object> GetUncommittedEvents() => _uncommittedEvents.AsReadOnly();

    public void ClearUncommittedEvents() => _uncommittedEvents.Clear();

    protected void RaiseEvent<TEvent>(TEvent @event) where TEvent : notnull
    {
        ApplyEvent(@event);
        _uncommittedEvents.Add(@event);
    }

    public void ReplayEvents(IEnumerable<object> events)
    {
        foreach (var @event in events)
        {
            ApplyEvent(@event);
            Version++;
        }
    }

    private void ApplyEvent(object @event)
    {
        var key = (GetType(), @event.GetType());
        var method = MethodCache.GetOrAdd(key, static k =>
            k.Aggregate.GetMethod("Apply", BindingFlags.Public | BindingFlags.Instance, [k.Event])
            ?? throw new InvalidOperationException($"{k.Aggregate.Name} has no Apply({k.Event.Name}) method."));

        method.Invoke(this, [@event]);
    }
}
