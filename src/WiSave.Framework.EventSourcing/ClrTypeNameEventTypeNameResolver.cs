namespace WiSave.Framework.EventSourcing;

public sealed class ClrTypeNameEventTypeNameResolver : IEventTypeNameResolver
{
    public string Resolve(Type eventType) => eventType.Name;
}
