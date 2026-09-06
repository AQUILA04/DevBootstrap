using System.Text.Json.Serialization;

namespace StackPilot.Models;

public sealed class CatalogDocument
{
    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("profiles")]
    public List<ProfileEntry> Profiles { get; set; } = new();

    [JsonPropertyName("packages")]
    public List<PackageEntry> Packages { get; set; } = new();
}

public sealed class ProfileEntry
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("packages")]
    public List<string> Packages { get; set; } = new();

    public override string ToString() => Name;
}

public sealed class PackageEntry
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("source")]
    public string Source { get; set; } = "winget";

    [JsonPropertyName("detect")]
    public string? Detect { get; set; }

    [JsonPropertyName("installer")]
    public string? Installer { get; set; }

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();

    [JsonPropertyName("default")]
    public bool Default { get; set; }

    [JsonPropertyName("requiresAdmin")]
    public bool RequiresAdmin { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    public string DisplayLabel => RequiresAdmin ? $"{Name} [admin]" : Name;
}
