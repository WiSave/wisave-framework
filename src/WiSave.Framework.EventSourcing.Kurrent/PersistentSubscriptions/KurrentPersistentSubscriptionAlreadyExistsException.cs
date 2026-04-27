namespace WiSave.Framework.EventSourcing.Kurrent.PersistentSubscriptions;

public sealed class KurrentPersistentSubscriptionAlreadyExistsException(string groupName, Exception? innerException = null)
    : Exception($"Persistent subscription group '{groupName}' already exists.", innerException);
