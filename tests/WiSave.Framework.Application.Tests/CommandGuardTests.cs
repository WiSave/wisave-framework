using WiSave.Framework.Application;

namespace WiSave.Framework.Application.Tests;

public class CommandGuardTests
{
    [Fact]
    public void Require_returns_first_failure_and_short_circuits()
    {
        var evaluatedAfterFailure = false;

        var guard = CommandGuard.Ok
            .Require(() => true, "first")
            .Require(() => false, "second")
            .Require(() =>
            {
                evaluatedAfterFailure = true;
                return false;
            }, "third");

        Assert.True(guard.HasFailed(out var error));
        Assert.Equal("second", error);
        Assert.False(evaluatedAfterFailure);
    }

    [Fact]
    public async Task RequireAsync_chains_with_sync_requirements()
    {
        var guard = await CommandGuard.Ok
            .Require(() => true, "sync")
            .RequireAsync(() => Task.FromResult(false), "async")
            .Require(() => false, "after async");

        Assert.True(guard.HasFailed(out var error));
        Assert.Equal("async", error);
    }
}
