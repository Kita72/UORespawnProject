namespace UORespawnApp.Scripts.Services.Platform;

/// <summary>
/// Abstraction over platform-specific preferences storage.
/// MAUI: Uses Microsoft.Maui.Storage.Preferences (Registry/NSUserDefaults).
/// Linux: Uses JSON file in ~/.config/UORespawn/.
/// </summary>
public interface IPreferencesService
{
    T Get<T>(string key, T defaultValue);
    void Set<T>(string key, T value);
    bool ContainsKey(string key);
    void Remove(string key);
    void Clear();
}
