namespace WiSave.Framework.EventSourcing.Kurrent.PersistentSubscriptions;

public sealed class KurrentPersistentSubscriptionDroppedException(
    string groupName,
    string reason,
    Exception? innerException = null)
    : Exception($"Persistent subscription group '{groupName}' dropped. Reason: {reason}.", innerException)
{
    public string GroupName { get; } = groupName;
    public string Reason { get; } = reason;
}
