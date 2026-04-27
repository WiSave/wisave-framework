namespace WiSave.Framework.EventSourcing;

public interface IEventTypeNameResolver
{
    string Resolve(Type eventType);
}
