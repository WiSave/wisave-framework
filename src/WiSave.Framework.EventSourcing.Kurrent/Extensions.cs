using EventStore.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WiSave.Framework.EventSourcing.Kurrent.Configuration;
using WiSave.Framework.EventSourcing.Kurrent.Forwarding;
using WiSave.Framework.EventSourcing.Kurrent.PersistentSubscriptions;

namespace WiSave.Framework.EventSourcing.Kurrent;

public static class Extensions
{
    public static IServiceCollection AddKurrentEventStore(
        this IServiceCollection services,
        string connectionString)
    {
        var settings = EventStoreClientSettings.Create(connectionString);
        services.AddSingleton(new EventStoreClient(settings));
        services.AddSingleton(new EventStorePersistentSubscriptionsClient(settings));
        services.AddSingleton<IEventTypeNameResolver, ClrTypeNameEventTypeNameResolver>();

        return services;
    }

    public static IServiceCollection AddKurrentForwarding(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<KurrentForwarderOptions>(configuration.GetSection("KurrentForwarder"));
        services.AddSingleton<IKurrentPersistentSubscriptionClient, EventStorePersistentSubscriptionClientAdapter>();
        services.AddSingleton<KurrentSubscriptionBootstrapper>();
        services.AddHostedService<KurrentToMassTransitForwarder>();

        return services;
    }
}
