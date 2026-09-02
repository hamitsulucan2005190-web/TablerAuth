namespace TablerAuth.Application.Admin;

public sealed class AdminActionResult
{
    public bool Succeeded { get; init; }

    public IReadOnlyList<string> Errors { get; init; } = [];

    public static AdminActionResult Success() => new() { Succeeded = true };

    public static AdminActionResult Fail(params string[] errors) =>
        new() { Succeeded = false, Errors = errors };
}
