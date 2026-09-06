using System.Text.Json;
using StackPilot.Models;

namespace StackPilot.Services;

public static class CatalogLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static string ResolveCatalogPath()
    {
        var baseDir = AppContext.BaseDirectory;
        var beside = Path.Combine(baseDir, "catalog.json");
        if (File.Exists(beside))
        {
            return beside;
        }

        // Dev: running from bin/.../net8.0-windows/win-x64
        var repoCandidate = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "..", "catalog.json"));
        if (File.Exists(repoCandidate))
        {
            return repoCandidate;
        }

        throw new FileNotFoundException("catalog.json introuvable a cote de StackPilot.exe.");
    }

    public static CatalogDocument Load()
    {
        var path = ResolveCatalogPath();
        var json = File.ReadAllText(path);
        var catalog = JsonSerializer.Deserialize<CatalogDocument>(json, JsonOptions)
            ?? throw new InvalidOperationException("Catalogue invalide.");
        if (catalog.Packages.Count == 0)
        {
            throw new InvalidOperationException("Catalogue vide.");
        }

        ValidateProfiles(catalog);
        return catalog;
    }

    internal static void ValidateProfiles(CatalogDocument catalog)
    {
        if (catalog.Profiles.Count == 0)
        {
            return;
        }

        var packageKeys = new HashSet<string>(
            catalog.Packages.Select(p => p.Key),
            StringComparer.OrdinalIgnoreCase);

        foreach (var profile in catalog.Profiles)
        {
            if (string.IsNullOrWhiteSpace(profile.Key))
            {
                throw new InvalidOperationException("Profil sans cle (key) dans le catalogue.");
            }

            var missing = profile.Packages
                .Where(key => !string.IsNullOrWhiteSpace(key) && !packageKeys.Contains(key))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (missing.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Profil '{profile.Key}': cles inconnues: {string.Join(", ", missing)}.");
            }
        }
    }
}
