namespace WiSave.Framework.Application;

public readonly struct CommandGuard
{
    private readonly string? _error;

    private CommandGuard(string? error) => _error = error;

    public static CommandGuard Ok => new(null);

    public CommandGuard Require(Func<bool> condition, string error)
        => _error is not null ? this : condition() ? this : new(error);

    public async Task<CommandGuard> RequireAsync(Func<Task<bool>> condition, string error)
        => _error is not null ? this : await condition() ? this : new(error);

    public bool HasFailed(out string error)
    {
        error = _error!;
        return _error is not null;
    }
}

public static class CommandGuardExtensions
{
    public static async Task<CommandGuard> Require(this Task<CommandGuard> guard, Func<bool> condition, string error)
        => (await guard).Require(condition, error);

    public static async Task<CommandGuard> RequireAsync(this Task<CommandGuard> guard, Func<Task<bool>> condition, string error)
        => await (await guard).RequireAsync(condition, error);
}
