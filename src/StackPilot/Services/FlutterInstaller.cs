using System.IO.Compression;
using System.Text.Json;
using StackPilot.Models;

namespace StackPilot.Services;

/// <summary>
/// Flutter n'est pas publie de facon fiable sur winget (SDK portable / scripts .bat).
/// On telecharge le canal stable officiel et on ajoute bin au PATH utilisateur.
/// </summary>
public sealed class FlutterInstaller
{
    private const string ReleasesUrl =
        "https://storage.googleapis.com/flutter_infra_release/releases/releases_windows.json";

    public static string InstallRoot =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "flutter");

    public static string FlutterBatPath => Path.Combine(InstallRoot, "bin", "flutter.bat");

    public Task<bool> IsInstalledAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (File.Exists(FlutterBatPath))
        {
            return Task.FromResult(true);
        }

        var onPath = ProcessRunner.FindOnPath("flutter.bat") ?? ProcessRunner.FindOnPath("flutter");
        return Task.FromResult(onPath is not null);
    }

    public async Task<InstallResult> InstallAsync(
        PackageEntry package,
        Action<string>? log = null,
        CancellationToken ct = default)
    {
        log?.Invoke($"[*] Installation: {package.Name} (SDK Flutter stable)");

        try
        {
            if (await IsInstalledAsync(ct).ConfigureAwait(false))
            {
                EnsureUserPath(log);
                log?.Invoke($"[+] {package.Name}: deja present");
                return Ok(package, InstallStatus.Already, "Deja present");
            }

            using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(15) };
            log?.Invoke("[*] Lecture du catalogue de releases Flutter...");
            await using var releasesStream = await http.GetStreamAsync(ReleasesUrl, ct).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(releasesStream, cancellationToken: ct)
                .ConfigureAwait(false);

            if (!TryResolveStableArchive(doc.RootElement, out var baseUrl, out var archive, out var version))
            {
                return Fail(package, "Impossible de resoudre la release Flutter stable.");
            }

            var downloadUrl = $"{baseUrl.TrimEnd('/')}/{archive.TrimStart('/')}";
            log?.Invoke($"[*] Telechargement Flutter {version}...");
            log?.Invoke($"    {downloadUrl}");

            var tempDir = Path.Combine(Path.GetTempPath(), "stackpilot-flutter-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            var zipPath = Path.Combine(tempDir, "flutter_windows-stable.zip");

            try
            {
                await using (var remote = await http.GetStreamAsync(downloadUrl, ct).ConfigureAwait(false))
                await using (var file = File.Create(zipPath))
                {
                    await remote.CopyToAsync(file, ct).ConfigureAwait(false);
                }

                var extractParent = Path.GetDirectoryName(InstallRoot)!;
                Directory.CreateDirectory(extractParent);

                if (Directory.Exists(InstallRoot))
                {
                    log?.Invoke("[*] Suppression de l'ancien dossier Flutter local...");
                    Directory.Delete(InstallRoot, recursive: true);
                }

                log?.Invoke($"[*] Extraction vers {InstallRoot}...");
                ZipFile.ExtractToDirectory(zipPath, extractParent, overwriteFiles: true);

                // L'archive contient un dossier "flutter" a la racine.
                if (!File.Exists(FlutterBatPath))
                {
                    return Fail(package, $"Extraction incomplete: {FlutterBatPath} introuvable.");
                }

                EnsureUserPath(log);
                log?.Invoke($"[+] {package.Name}: installe ({version})");
                log?.Invoke("[!] Ouvre un nouveau terminal, puis lance: flutter doctor");
                return Ok(package, InstallStatus.Installed, $"OK ({version})");
            }
            finally
            {
                try
                {
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, recursive: true);
                    }
                }
                catch
                {
                    // best effort cleanup
                }
            }
        }
        catch (Exception ex)
        {
            log?.Invoke($"[x] {package.Name}: {ex.Message}");
            return Fail(package, ex.Message);
        }
    }

    private static bool TryResolveStableArchive(
        JsonElement root,
        out string baseUrl,
        out string archive,
        out string version)
    {
        baseUrl = "";
        archive = "";
        version = "";

        if (!root.TryGetProperty("base_url", out var baseUrlEl)
            || !root.TryGetProperty("current_release", out var current)
            || !current.TryGetProperty("stable", out var stableHashEl)
            || !root.TryGetProperty("releases", out var releases))
        {
            return false;
        }

        baseUrl = baseUrlEl.GetString() ?? "";
        var stableHash = stableHashEl.GetString() ?? "";
        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(stableHash))
        {
            return false;
        }

        foreach (var release in releases.EnumerateArray())
        {
            var hash = release.TryGetProperty("hash", out var h) ? h.GetString() : null;
            var channel = release.TryGetProperty("channel", out var c) ? c.GetString() : null;
            if (!string.Equals(hash, stableHash, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(channel, "stable", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            archive = release.TryGetProperty("archive", out var a) ? a.GetString() ?? "" : "";
            version = release.TryGetProperty("version", out var v) ? v.GetString() ?? "" : "";
            return !string.IsNullOrWhiteSpace(archive);
        }

        return false;
    }

    private static void EnsureUserPath(Action<string>? log)
    {
        var bin = Path.Combine(InstallRoot, "bin");
        if (!Directory.Exists(bin))
        {
            return;
        }

        const string name = "Path";
        var current = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User) ?? "";
        var parts = current.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Any(p => string.Equals(p, bin, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        var updated = string.IsNullOrWhiteSpace(current) ? bin : current.TrimEnd(';') + ";" + bin;
        Environment.SetEnvironmentVariable(name, updated, EnvironmentVariableTarget.User);
        log?.Invoke($"[*] PATH utilisateur mis a jour (+ {bin})");
    }

    private static InstallResult Ok(PackageEntry package, InstallStatus status, string message) => new()
    {
        Key = package.Key,
        Name = package.Name,
        Id = package.Id,
        Status = status,
        Message = message
    };

    private static InstallResult Fail(PackageEntry package, string message) => new()
    {
        Key = package.Key,
        Name = package.Name,
        Id = package.Id,
        Status = InstallStatus.Failed,
        Message = message
    };
}
