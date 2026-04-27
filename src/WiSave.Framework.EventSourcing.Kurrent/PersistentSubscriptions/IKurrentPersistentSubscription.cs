namespace WiSave.Framework.EventSourcing.Kurrent.PersistentSubscriptions;

public interface IKurrentPersistentSubscription : IAsyncDisposable
{
    IAsyncEnumerable<KurrentPersistentSubscriptionMessage> Messages { get; }
}
