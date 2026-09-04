namespace StackPilot.Services;

public sealed class WingetClient
{
    private readonly string _wingetPath;

    public WingetClient()
    {
        _wingetPath = ProcessRunner.FindOnPath("winget.exe")
            ?? throw new InvalidOperationException(
                "winget est introuvable. Installez 'App Installer' depuis le Microsoft Store.");
    }

    public async Task EnsureSourceAsync(Action<string>? log = null, CancellationToken ct = default)
    {
        log?.Invoke("[*] Mise a jour des sources winget...");
        await ProcessRunner.RunAsync(
            _wingetPath,
            new[] { "source", "update", "--disable-interactivity" },
            log,
            ct).ConfigureAwait(false);
    }

    public async Task<bool> IsInstalledAsync(string packageId, CancellationToken ct = default)
    {
        var result = await ProcessRunner.RunAsync(
            _wingetPath,
            new[] { "list", "--id", packageId, "--exact", "--accept-source-agreements" },
            cancellationToken: ct).ConfigureAwait(false);

        if (result.ExitCode != 0)
        {
            return false;
        }

        return result.Output.Contains(packageId, StringComparison.OrdinalIgnoreCase);
    }

    public async Task<ProcessResult> InstallAsync(
        string packageId,
        Action<string>? log = null,
        CancellationToken ct = default)
    {
        return await ProcessRunner.RunAsync(
            _wingetPath,
            new[]
            {
                "install",
                "--id", packageId,
                "--exact",
                "--accept-package-agreements",
                "--accept-source-agreements",
                "--disable-interactivity"
            },
            log,
            ct).ConfigureAwait(false);
    }

    public static bool IsAlreadyPresentExitCode(int exitCode)
        => exitCode is 0 or -1978335189;
}
