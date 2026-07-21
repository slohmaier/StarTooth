using System.Text.Json;

namespace StarTooth;

/// <summary>
/// The starred devices, persisted as a flat list of device keys in %APPDATA%\StarTooth.
/// Deliberately tolerant: a corrupt or missing file just means "no favourites yet".
/// </summary>
internal sealed class Favorites
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _path;
    private readonly HashSet<string> _keys;

    internal Favorites()
    {
        string dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StarTooth");
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "favorites.json");
        _keys = Load(_path);
    }

    internal bool IsEmpty => _keys.Count == 0;

    internal bool Contains(string key) => _keys.Contains(key);

    internal void SetFavorite(string key, bool isFavorite)
    {
        bool changed = isFavorite ? _keys.Add(key) : _keys.Remove(key);
        if (changed)
            Save();
    }

    private static HashSet<string> Load(string path)
    {
        try
        {
            if (!File.Exists(path))
                return [];
            var keys = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(path));
            return keys is null ? [] : new HashSet<string>(keys);
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    private void Save()
    {
        try
        {
            File.WriteAllText(_path, JsonSerializer.Serialize(_keys.ToList(), JsonOptions));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Losing a star is not worth interrupting the user with a dialog.
        }
    }
}
