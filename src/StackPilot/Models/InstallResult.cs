namespace StackPilot.Models;

public enum InstallStatus
{
    Pending,
    Installed,
    Already,
    Failed,
    Skipped
}

public sealed class InstallResult
{
    public required string Key { get; init; }
    public required string Name { get; init; }
    public required string Id { get; init; }
    public InstallStatus Status { get; init; }
    public string Message { get; init; } = "";
}
