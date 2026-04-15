namespace UORespawnApp.Scripts.Services.Platform;

/// <summary>
/// MAUI implementation of IPreferencesService.
/// Delegates to Microsoft.Maui.Storage.Preferences (Registry on Windows, NSUserDefaults on macOS).
/// </summary>
public class MauiPreferencesService : IPreferencesService
{
    public T Get<T>(string key, T defaultValue) => Preferences.Get(key, defaultValue);
    public void Set<T>(string key, T value) => Preferences.Set(key, value);
    public bool ContainsKey(string key) => Preferences.ContainsKey(key);
    public void Remove(string key) => Preferences.Remove(key);
    public void Clear() => Preferences.Clear();
}
