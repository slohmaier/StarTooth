using System.Text.Json;
using System.Text.Json.Serialization;

namespace StarTooth;

internal enum ThemeMode
{
    System,
    Light,
    Dark,
}

/// <summary>
/// User preferences, stored next to the favourites. Autostart is deliberately absent: it lives in
/// the registry, and keeping a second copy here would let the two drift apart.
/// </summary>
internal sealed class Settings
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Culture name, or empty to follow the Windows display language.</summary>
    public string Language { get; set; } = string.Empty;

    public ThemeMode Theme { get; set; } = ThemeMode.System;

    [JsonIgnore]
    internal static string FilePath
    {
        get
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StarTooth");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "settings.json");
        }
    }

    internal static Settings Load()
    {
        try
        {
            string path = FilePath;
            if (!File.Exists(path))
                return new Settings();
            return JsonSerializer.Deserialize<Settings>(File.ReadAllText(path), JsonOptions)
                   ?? new Settings();
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            // Unreadable settings fall back to the defaults rather than blocking startup.
            return new Settings();
        }
    }

    internal void Save()
    {
        try
        {
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, JsonOptions));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }
}
