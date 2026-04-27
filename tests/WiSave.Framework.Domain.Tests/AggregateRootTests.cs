using WiSave.Framework.Domain;

namespace WiSave.Framework.Domain.Tests;

public class AggregateRootTests
{
    [Fact]
    public void RaiseEvent_applies_event_and_tracks_uncommitted_event()
    {
        var aggregate = TestAggregate.Create("aggregate-1", "opened");

        Assert.Equal("aggregate-1", aggregate.Id);
        Assert.Equal("opened", aggregate.Name);
        Assert.Equal(-1, aggregate.Version);
        Assert.IsType<TestAggregateOpened>(Assert.Single(aggregate.GetUncommittedEvents()));
    }

    [Fact]
    public void ReplayEvents_applies_events_and_advances_version()
    {
        var aggregate = new TestAggregate();

        aggregate.ReplayEvents([
            new TestAggregateOpened("aggregate-1", "opened"),
            new TestAggregateRenamed("renamed"),
        ]);

        Assert.Equal("aggregate-1", aggregate.Id);
        Assert.Equal("renamed", aggregate.Name);
        Assert.Equal(1, aggregate.Version);
        Assert.Empty(aggregate.GetUncommittedEvents());
    }

    [Fact]
    public void ReplayEvents_throws_when_apply_method_is_missing()
    {
        var aggregate = new TestAggregate();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            aggregate.ReplayEvents([new TestAggregateUnsupported()]));

        Assert.Equal("TestAggregate has no Apply(TestAggregateUnsupported) method.", ex.Message);
    }

    private sealed class TestAggregate : AggregateRoot<string>, IAggregateStream<string>
    {
        public string Name { get; private set; } = string.Empty;

        public static string ToStreamId(string id) => $"test-{id}";

        public static TestAggregate Create(string id, string name)
        {
            var aggregate = new TestAggregate();
            aggregate.RaiseEvent(new TestAggregateOpened(id, name));
            return aggregate;
        }

        public void Apply(TestAggregateOpened e)
        {
            Id = e.Id;
            Name = e.Name;
        }

        public void Apply(TestAggregateRenamed e)
        {
            Name = e.Name;
        }
    }

    private sealed record TestAggregateOpened(string Id, string Name);
    private sealed record TestAggregateRenamed(string Name);
    private sealed record TestAggregateUnsupported;
}
