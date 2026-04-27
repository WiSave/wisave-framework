using System.Text.Json;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WiSave.Framework.EventSourcing.Kurrent.Configuration;
using WiSave.Framework.EventSourcing.Kurrent.Forwarding;
using WiSave.Framework.EventSourcing.Kurrent.PersistentSubscriptions;

namespace WiSave.Framework.EventSourcing.Kurrent.Tests;

public class KurrentToMassTransitForwarderTests
{
    [Fact]
    public async Task HandleEventAsync_publishes_registered_event_and_acks()
    {
        var publishEndpoint = new RecordingPublishEndpoint();
        var message = new SampleForwardedEvent("sample-1");
        var actions = new FakeSubscriptionActions();
        var sut = CreateForwarder(publishEndpoint, ["sample-"]);

        var handled = await sut.HandleEventAsync(
            new KurrentCommittedEvent(
                Guid.NewGuid(),
                nameof(SampleForwardedEvent),
                "sample-1",
                JsonSerializer.SerializeToUtf8Bytes(message),
                actions),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(message, Assert.Single(publishEndpoint.Published));
        Assert.Equal(1, actions.AckCalls);
    }

    [Fact]
    public async Task HandleEventAsync_skips_event_outside_configured_stream_prefixes()
    {
        var publishEndpoint = new RecordingPublishEndpoint();
        var actions = new FakeSubscriptionActions();
        var sut = CreateForwarder(publishEndpoint, ["sample-"]);

        var handled = await sut.HandleEventAsync(
            new KurrentCommittedEvent(
                Guid.NewGuid(),
                nameof(SampleForwardedEvent),
                "other-1",
                "{}"u8.ToArray(),
                actions),
            CancellationToken.None);

        Assert.False(handled);
        Assert.Empty(publishEndpoint.Published);
        Assert.Equal(1, actions.SkipCalls);
    }

    private static KurrentToMassTransitForwarder CreateForwarder(
        IPublishEndpoint publishEndpoint,
        string[] streamPrefixes)
    {
        var options = Options.Create(new KurrentForwarderOptions
        {
            GroupName = "test-forwarder",
            StreamPrefixes = streamPrefixes,
        });

        return new KurrentToMassTransitForwarder(
            client: new FakePersistentSubscriptionClient(),
            bootstrapper: new KurrentSubscriptionBootstrapper(
                new FakePersistentSubscriptionClient(),
                options,
                NullLogger<KurrentSubscriptionBootstrapper>.Instance),
            CreateScopeFactory(publishEndpoint),
            AssemblyEventTypeRegistry.FromAssemblies(
                [typeof(SampleForwardedEvent).Assembly],
                type => type == typeof(SampleForwardedEvent)),
            options,
            NullLogger<KurrentToMassTransitForwarder>.Instance);
    }

    private static IServiceScopeFactory CreateScopeFactory(IPublishEndpoint publishEndpoint)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => publishEndpoint);

        return services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }
}

public sealed record SampleForwardedEvent(string Id);

internal sealed class FakePersistentSubscriptionClient : IKurrentPersistentSubscriptionClient
{
    public Task CreateToAllAsync(string groupName, KurrentPersistentSubscriptionCreateOptions options, CancellationToken ct) =>
        Task.CompletedTask;

    public Task<IKurrentPersistentSubscription> SubscribeToAllAsync(string groupName, CancellationToken ct) =>
        Task.FromResult<IKurrentPersistentSubscription>(new FakePersistentSubscription());

    private sealed class FakePersistentSubscription : IKurrentPersistentSubscription
    {
        public IAsyncEnumerable<KurrentPersistentSubscriptionMessage> Messages => ReadMessagesAsync();

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        private static async IAsyncEnumerable<KurrentPersistentSubscriptionMessage> ReadMessagesAsync()
        {
            await Task.CompletedTask;
            yield break;
        }
    }
}

internal sealed class FakeSubscriptionActions : IKurrentSubscriptionActions
{
    public int AckCalls { get; private set; }
    public int SkipCalls { get; private set; }

    public Task AckAsync(CancellationToken ct)
    {
        AckCalls++;
        return Task.CompletedTask;
    }

    public Task RetryAsync(string reason, CancellationToken ct) => Task.CompletedTask;
    public Task ParkAsync(string reason, CancellationToken ct) => Task.CompletedTask;

    public Task SkipAsync(string reason, CancellationToken ct)
    {
        SkipCalls++;
        return Task.CompletedTask;
    }
}

internal sealed class RecordingPublishEndpoint : IPublishEndpoint
{
    public List<object> Published { get; } = [];

    public ConnectHandle ConnectPublishObserver(IPublishObserver observer) => throw new NotSupportedException();

    public Task Publish<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        Published.Add(message);
        return Task.CompletedTask;
    }

    public Task Publish<T>(T message, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = default) where T : class
    {
        Published.Add(message);
        return Task.CompletedTask;
    }

    public Task Publish<T>(T message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default) where T : class
    {
        Published.Add(message);
        return Task.CompletedTask;
    }

    public Task Publish<T>(object values, CancellationToken cancellationToken = default) where T : class => throw new NotSupportedException();
    public Task Publish<T>(object values, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = default) where T : class => throw new NotSupportedException();
    public Task Publish<T>(object values, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default) where T : class => throw new NotSupportedException();
    public Task Publish(object message, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task Publish(object message, Type messageType, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task Publish(object message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
    {
        Published.Add(message);
        return Task.CompletedTask;
    }

    public Task Publish(object message, Type messageType, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
    {
        Published.Add(message);
        return Task.CompletedTask;
    }
}
