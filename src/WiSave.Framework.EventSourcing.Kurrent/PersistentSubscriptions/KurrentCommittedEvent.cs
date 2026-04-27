namespace WiSave.Framework.EventSourcing.Kurrent.PersistentSubscriptions;

public sealed record KurrentCommittedEvent(
    Guid EventId,
    string EventType,
    string StreamId,
    byte[] Data,
    IKurrentSubscriptionActions Actions);
