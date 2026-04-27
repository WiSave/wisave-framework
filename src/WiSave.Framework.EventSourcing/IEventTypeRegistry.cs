namespace WiSave.Framework.EventSourcing;

public interface IEventTypeRegistry
{
    Type? Resolve(string eventTypeName);
}
