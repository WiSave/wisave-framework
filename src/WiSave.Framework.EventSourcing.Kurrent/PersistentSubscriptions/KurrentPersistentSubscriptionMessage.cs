namespace WiSave.Framework.EventSourcing.Kurrent.PersistentSubscriptions;

public abstract record KurrentPersistentSubscriptionMessage
{
    public sealed record Confirmation(string SubscriptionId) : KurrentPersistentSubscriptionMessage;

    public sealed record Event(KurrentCommittedEvent CommittedEvent) : KurrentPersistentSubscriptionMessage;
}
