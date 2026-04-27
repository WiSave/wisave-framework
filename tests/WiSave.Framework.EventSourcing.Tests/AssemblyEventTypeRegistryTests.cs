using WiSave.Framework.EventSourcing;
using WiSave.Framework.EventSourcing.Tests.Events;

namespace WiSave.Framework.EventSourcing.Tests;

public class AssemblyEventTypeRegistryTests
{
    [Fact]
    public void Resolve_returns_exported_event_type_from_configured_assembly()
    {
        var sut = AssemblyEventTypeRegistry.FromAssemblies(
            [typeof(SampleEvent).Assembly],
            type => type.Namespace?.EndsWith(".Events", StringComparison.Ordinal) == true);

        Assert.Equal(typeof(SampleEvent), sut.Resolve(nameof(SampleEvent)));
    }

    [Fact]
    public void Resolve_returns_null_for_unregistered_event_type()
    {
        var sut = AssemblyEventTypeRegistry.FromAssemblies(
            [typeof(SampleEvent).Assembly],
            type => type.Namespace?.EndsWith(".Events", StringComparison.Ordinal) == true);

        Assert.Null(sut.Resolve("MissingEvent"));
    }
}
