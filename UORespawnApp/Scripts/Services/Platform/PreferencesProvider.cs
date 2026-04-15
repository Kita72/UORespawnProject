namespace UORespawnApp.Scripts.Services.Platform;

/// <summary>
/// Static facade over IPreferencesService for use by Settings and other static classes.
/// Must be initialized at startup before any preferences access.
/// Drop-in replacement for Microsoft.Maui.Storage.Preferences static calls.
/// </summary>
public static class PreferencesProvider
{
    private static IPreferencesService? _service;

    /// <summary>
    /// Initializes the provider with a platform-specific implementation.
    /// Call this once at startup (MauiProgram.cs or Linux Program.cs).
    /// </summary>
    public static void Initialize(IPreferencesService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public static T Get<T>(string key, T defaultValue)
    {
        EnsureInitialized();
        return _service!.Get(key, defaultValue);
    }

    public static void Set<T>(string key, T value)
    {
        EnsureInitialized();
        _service!.Set(key, value);
    }

    public static bool ContainsKey(string key)
    {
        EnsureInitialized();
        return _service!.ContainsKey(key);
    }

    public static void Remove(string key)
    {
        EnsureInitialized();
        _service!.Remove(key);
    }

    public static void Clear()
    {
        EnsureInitialized();
        _service!.Clear();
    }

    private static void EnsureInitialized()
    {
        if (_service is null)
            throw new InvalidOperationException(
                "PreferencesProvider not initialized. Call PreferencesProvider.Initialize() at startup.");
    }
}
