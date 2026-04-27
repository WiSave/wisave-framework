using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WiSave.Framework.EventSourcing.Kurrent.PersistentSubscriptions;

namespace WiSave.Framework.EventSourcing.Kurrent.Configuration;

public sealed class KurrentSubscriptionBootstrapper(
    IKurrentPersistentSubscriptionClient client,
    IOptions<KurrentForwarderOptions> options,
    ILogger<KurrentSubscriptionBootstrapper> logger)
{
    public async Task EnsureCreatedAsync(CancellationToken ct)
    {
        try
        {
            await client.CreateToAllAsync(
                options.Value.GroupName,
                new KurrentPersistentSubscriptionCreateOptions(
                    options.Value.FromStartWhenCreated,
                    options.Value.MaxSubscriberCount,
                    options.Value.ConsumerStrategyName),
                ct);
        }
        catch (KurrentPersistentSubscriptionAlreadyExistsException)
        {
            logger.LogInformation("Persistent subscription group {GroupName} already exists", options.Value.GroupName);
        }
    }
}
