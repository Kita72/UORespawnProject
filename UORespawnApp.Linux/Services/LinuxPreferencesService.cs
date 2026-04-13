using System.Text.Json;
using UORespawnApp.Scripts.Services.Platform;

namespace UORespawnApp;

/// <summary>
/// Linux implementation of IPreferencesService.
/// Persists settings as a JSON file in ~/.config/UORespawn/preferences.json.
/// Thread-safe with read/write locking.
/// </summary>
public class LinuxPreferencesService : IPreferencesService
{
    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "UORespawn");

    private static readonly string PrefsFile = Path.Combine(ConfigDir, "preferences.json");

    private readonly Dictionary<string, JsonElement> _data;
    private readonly object _lock = new();

    public LinuxPreferencesService()
    {
        Directory.CreateDirectory(ConfigDir);
        _data = LoadFromDisk();
    }

    public T Get<T>(string key, T defaultValue)
    {
        lock (_lock)
        {
            if (!_data.TryGetValue(key, out var element))
                return defaultValue;

            try
            {
                // Handle type conversions from JsonElement
                var targetType = typeof(T);

                if (targetType == typeof(string))
                    return (T)(object)element.GetString()!;
                if (targetType == typeof(int))
                    return (T)(object)element.GetInt32();
                if (targetType == typeof(double))
                    return (T)(object)element.GetDouble();
                if (targetType == typeof(bool))
                    return (T)(object)element.GetBoolean();
                if (targetType == typeof(float))
                    return (T)(object)element.GetSingle();
                if (targetType == typeof(long))
                    return (T)(object)element.GetInt64();

                // Fallback: try deserialize
                return element.Deserialize<T>() ?? defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }
    }

    public void Set<T>(string key, T value)
    {
        lock (_lock)
        {
            // Serialize the value to a JsonElement for storage
            var json = JsonSerializer.Serialize(value);
            _data[key] = JsonDocument.Parse(json).RootElement.Clone();
            SaveToDisk();
        }
    }

    public bool ContainsKey(string key)
    {
        lock (_lock)
        {
            return _data.ContainsKey(key);
        }
    }

    public void Remove(string key)
    {
        lock (_lock)
        {
            if (_data.Remove(key))
                SaveToDisk();
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _data.Clear();
            SaveToDisk();
        }
    }

    private Dictionary<string, JsonElement> LoadFromDisk()
    {
        try
        {
            if (File.Exists(PrefsFile))
            {
                var json = File.ReadAllText(PrefsFile);
                var doc = JsonDocument.Parse(json);
                var result = new Dictionary<string, JsonElement>();
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    result[prop.Name] = prop.Value.Clone();
                }
                return result;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to load preferences: {ex.Message}");
        }

        return [];
    }

    private void SaveToDisk()
    {
        try
        {
            var dict = new Dictionary<string, object?>();
            foreach (var kvp in _data)
            {
                dict[kvp.Key] = ConvertJsonElement(kvp.Value);
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(dict, options);
            File.WriteAllText(PrefsFile, json);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to save preferences: {ex.Message}");
        }
    }

    private static object? ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.GetRawText()
        };
    }
}
