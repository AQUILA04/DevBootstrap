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

        return catalog;
    }
}
