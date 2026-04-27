using WiSave.Framework.EventSourcing.Kurrent.Configuration;

namespace WiSave.Framework.EventSourcing.Kurrent.Tests;

public class KurrentForwarderOptionsTests
{
    [Fact]
    public void Defaults_do_not_include_service_specific_stream_prefixes()
    {
        var sut = new KurrentForwarderOptions();

        Assert.Equal("kurrent-forwarder", sut.GroupName);
        Assert.Empty(sut.StreamPrefixes);
    }
}
