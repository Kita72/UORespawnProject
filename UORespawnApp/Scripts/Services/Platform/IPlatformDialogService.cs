namespace UORespawnApp.Scripts.Services.Platform;

/// <summary>
/// Platform-agnostic file result returned by dialog services.
/// </summary>
public record PlatformFileResult(string FullPath, string FileName);

/// <summary>
/// Abstraction over platform-specific file/folder dialogs.
/// MAUI: Uses Windows.Storage.Pickers (WinRT) and MAUI FilePicker.
/// Linux: Uses zenity/kdialog native dialogs.
/// </summary>
public interface IPlatformDialogService
{
    /// <summary>
    /// Shows a folder picker dialog and returns the selected folder path, or null if cancelled.
    /// </summary>
    Task<string?> PickFolderAsync(string? suggestedStartLocation = null);

    /// <summary>
    /// Shows a file picker dialog for selecting a single file.
    /// </summary>
    /// <param name="title">Dialog title</param>
    /// <param name="allowedExtensions">File extensions to filter (e.g., ".zip", ".png")</param>
    /// <returns>Selected file info, or null if cancelled</returns>
    Task<PlatformFileResult?> PickFileAsync(string title, IEnumerable<string> allowedExtensions);

    /// <summary>
    /// Shows a save file dialog.
    /// </summary>
    /// <param name="suggestedFileName">Default file name</param>
    /// <param name="filterName">Filter display name (e.g., "ZIP Archive")</param>
    /// <param name="extensions">Allowed extensions</param>
    /// <returns>Selected save path, or null if cancelled</returns>
    Task<string?> SaveFileAsync(string suggestedFileName, string filterName, IEnumerable<string> extensions);

    /// <summary>
    /// Opens the given path in the platform's default file manager.
    /// Windows: explorer.exe, macOS: open, Linux: xdg-open
    /// </summary>
    void OpenInFileManager(string path);

    /// <summary>
    /// Opens a bundled resource file from the app package.
    /// MAUI: Uses FileSystem.OpenAppPackageFileAsync.
    /// Linux: Reads from app base directory.
    /// </summary>
    Task<Stream> OpenAppResourceAsync(string relativePath);
}
