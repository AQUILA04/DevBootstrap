using StackPilot.Models;

namespace StackPilot.Services;

public sealed class InstallOrchestrator
{
    private readonly WingetClient _winget;
    private readonly WslInstaller _wsl;

    public InstallOrchestrator()
    {
        _winget = new WingetClient();
        _wsl = new WslInstaller(_winget);
    }

    public static IReadOnlyList<PackageEntry> OrderForInstall(IEnumerable<PackageEntry> selected)
    {
        return selected
            .OrderBy(p => p.Key switch
            {
                "wsl" => 0,
                "docker" => 1,
                "antigravity-ide" => 2,
                "antigravity" => 3,
                _ => 4
            })
            .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task EnsureReadyAsync(Action<string>? log = null, CancellationToken ct = default)
    {
        await _winget.EnsureSourceAsync(log, ct).ConfigureAwait(false);
    }

    public async Task<InstallResult> InstallAsync(
        PackageEntry package,
        bool checkInstalled = true,
        Action<string>? log = null,
        CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(package.Notes))
        {
            log?.Invoke($"[*] {package.Name}: {package.Notes}");
        }

        if (checkInstalled && await IsInstalledAsync(package, ct).ConfigureAwait(false))
        {
            log?.Invoke($"[+] {package.Name}: deja installe");
            return new InstallResult
            {
                Key = package.Key,
                Name = package.Name,
                Id = package.Id,
                Status = InstallStatus.Already,
                Message = "Deja installe"
            };
        }

        if (string.Equals(package.Installer, "wsl", StringComparison.OrdinalIgnoreCase)
            || string.Equals(package.Detect, "wsl", StringComparison.OrdinalIgnoreCase))
        {
            return await _wsl.InstallAsync(package, log, ct).ConfigureAwait(false);
        }

        log?.Invoke($"[*] Installation: {package.Name} ({package.Id})");
        var result = await _winget.InstallAsync(package.Id, log, ct).ConfigureAwait(false);

        if (result.ExitCode == 0)
        {
            log?.Invoke($"[+] {package.Name}: installe");
            return new InstallResult
            {
                Key = package.Key,
                Name = package.Name,
                Id = package.Id,
                Status = InstallStatus.Installed,
                Message = "OK"
            };
        }

        if (WingetClient.IsAlreadyPresentExitCode(result.ExitCode))
        {
            log?.Invoke($"[+] {package.Name}: deja present");
            return new InstallResult
            {
                Key = package.Key,
                Name = package.Name,
                Id = package.Id,
                Status = InstallStatus.Already,
                Message = "Deja present (winget)"
            };
        }

        log?.Invoke($"[x] {package.Name}: echec (code {result.ExitCode})");
        return new InstallResult
        {
            Key = package.Key,
            Name = package.Name,
            Id = package.Id,
            Status = InstallStatus.Failed,
            Message = $"winget exit code {result.ExitCode}"
        };
    }

    private async Task<bool> IsInstalledAsync(PackageEntry package, CancellationToken ct)
    {
        if (string.Equals(package.Detect, "wsl", StringComparison.OrdinalIgnoreCase))
        {
            return await _wsl.IsInstalledAsync(ct).ConfigureAwait(false);
        }

        return await _winget.IsInstalledAsync(package.Id, ct).ConfigureAwait(false);
    }
}
