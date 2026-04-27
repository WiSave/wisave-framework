namespace WiSave.Framework.EventSourcing.Kurrent.PersistentSubscriptions;

public sealed record KurrentPersistentSubscriptionCreateOptions(
    bool FromStartWhenCreated,
    int MaxSubscriberCount,
    string ConsumerStrategyName);
