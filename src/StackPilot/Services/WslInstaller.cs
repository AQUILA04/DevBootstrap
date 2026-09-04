using System.Text.RegularExpressions;
using StackPilot.Models;

namespace StackPilot.Services;

public sealed class WslInstaller
{
    private readonly WingetClient _winget;

    public WslInstaller(WingetClient winget)
    {
        _winget = winget;
    }

    public async Task<bool> IsInstalledAsync(CancellationToken ct = default)
    {
        var wsl = ProcessRunner.FindOnPath("wsl.exe");
        if (wsl is null)
        {
            return false;
        }

        var status = await ProcessRunner.RunAsync(wsl, new[] { "--status" }, cancellationToken: ct)
            .ConfigureAwait(false);
        if (status.ExitCode == 0)
        {
            return true;
        }

        if (Regex.IsMatch(status.Output, "(?i)not installed|pas install"))
        {
            return false;
        }

        var list = await ProcessRunner.RunAsync(wsl, new[] { "-l", "-q" }, cancellationToken: ct)
            .ConfigureAwait(false);
        if (list.ExitCode == 0)
        {
            return true;
        }

        if (await IsFeatureEnabledAsync("Microsoft-Windows-Subsystem-Linux", ct).ConfigureAwait(false))
        {
            return true;
        }

        return await _winget.IsInstalledAsync("Microsoft.WSL", ct).ConfigureAwait(false);
    }

    public async Task<InstallResult> InstallAsync(
        PackageEntry package,
        Action<string>? log = null,
        CancellationToken ct = default)
    {
        var rebootLikely = false;
        log?.Invoke($"[*] Installation: {package.Name}");

        if (await EnableFeaturesAsync(log, ct).ConfigureAwait(false))
        {
            rebootLikely = true;
        }

        var wingetResult = await _winget.InstallAsync(package.Id, log, ct).ConfigureAwait(false);
        if (!WingetClient.IsAlreadyPresentExitCode(wingetResult.ExitCode))
        {
            log?.Invoke($"[!] winget {package.Id} code {wingetResult.ExitCode} - tentative wsl --install");
            var wsl = ProcessRunner.FindOnPath("wsl.exe") ?? "wsl.exe";
            var fallback = await ProcessRunner.RunAsync(
                wsl,
                new[] { "--install", "--no-distribution" },
                log,
                ct).ConfigureAwait(false);

            if (fallback.ExitCode is not (0 or 3010))
            {
                return new InstallResult
                {
                    Key = package.Key,
                    Name = package.Name,
                    Id = package.Id,
                    Status = InstallStatus.Failed,
                    Message = $"winget={wingetResult.ExitCode}, wsl --install={fallback.ExitCode}"
                };
            }

            rebootLikely = true;
        }
        else if (wingetResult.ExitCode == -1978335189)
        {
            log?.Invoke("[+] WSL deja present via winget");
        }

        var wslExe = ProcessRunner.FindOnPath("wsl.exe");
        if (wslExe is not null)
        {
            await ProcessRunner.RunAsync(wslExe, new[] { "--set-default-version", "2" }, cancellationToken: ct)
                .ConfigureAwait(false);
        }

        if (await IsInstalledAsync(ct).ConfigureAwait(false))
        {
            var msg = rebootLikely ? "OK (reboot recommande)" : "OK";
            if (rebootLikely)
            {
                log?.Invoke("[!] WSL installe/active - redemarre Windows avant Docker Desktop.");
            }

            return new InstallResult
            {
                Key = package.Key,
                Name = package.Name,
                Id = package.Id,
                Status = InstallStatus.Installed,
                Message = msg
            };
        }

        if (rebootLikely)
        {
            log?.Invoke("[!] WSL active mais pas encore utilisable - un redemarrage est probablement requis.");
            return new InstallResult
            {
                Key = package.Key,
                Name = package.Name,
                Id = package.Id,
                Status = InstallStatus.Installed,
                Message = "OK (reboot requis)"
            };
        }

        return new InstallResult
        {
            Key = package.Key,
            Name = package.Name,
            Id = package.Id,
            Status = InstallStatus.Failed,
            Message = "WSL non detecte apres installation"
        };
    }

    private static async Task<bool> EnableFeaturesAsync(Action<string>? log, CancellationToken ct)
    {
        var features = new[]
        {
            "Microsoft-Windows-Subsystem-Linux",
            "VirtualMachinePlatform"
        };

        var changed = false;
        var dism = ProcessRunner.FindOnPath("dism.exe")
            ?? Path.Combine(Environment.SystemDirectory, "dism.exe");

        foreach (var name in features)
        {
            if (await IsFeatureEnabledAsync(name, ct).ConfigureAwait(false))
            {
                log?.Invoke($"[+] Composant Windows deja actif: {name}");
                continue;
            }

            log?.Invoke($"[*] Activation du composant Windows: {name}");
            var result = await ProcessRunner.RunAsync(
                dism,
                new[]
                {
                    "/online",
                    "/enable-feature",
                    $"/featurename:{name}",
                    "/all",
                    "/norestart"
                },
                log,
                ct).ConfigureAwait(false);

            if (result.ExitCode is 0 or 3010)
            {
                changed = true;
                log?.Invoke($"[+] Composant active (ou en attente de reboot): {name}");
            }
            else
            {
                log?.Invoke($"[!] Echec activation {name} (dism code {result.ExitCode})");
            }
        }

        return changed;
    }

    private static async Task<bool> IsFeatureEnabledAsync(string featureName, CancellationToken ct)
    {
        var dism = ProcessRunner.FindOnPath("dism.exe")
            ?? Path.Combine(Environment.SystemDirectory, "dism.exe");

        var result = await ProcessRunner.RunAsync(
            dism,
            new[] { "/online", "/get-featureinfo", $"/featurename:{featureName}" },
            cancellationToken: ct).ConfigureAwait(false);

        return Regex.IsMatch(result.Output, @"(?i)State\s*:\s*Enabled|Etat\s*:\s*Activ");
    }
}
