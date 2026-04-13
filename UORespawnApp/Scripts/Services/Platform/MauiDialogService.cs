using System.Diagnostics;

namespace UORespawnApp.Scripts.Services.Platform;

/// <summary>
/// MAUI implementation of IPlatformDialogService.
/// Uses WinRT pickers on Windows and MAUI FilePicker on macOS.
/// </summary>
public class MauiDialogService : IPlatformDialogService
{
    public async Task<string?> PickFolderAsync(string? suggestedStartLocation = null)
    {
#if WINDOWS
        var folderPicker = new Windows.Storage.Pickers.FolderPicker();
        var window = Application.Current?.Windows[0]?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
        if (window != null)
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);
        }
        folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.ComputerFolder;
        folderPicker.FileTypeFilter.Add("*");
        var folder = await folderPicker.PickSingleFolderAsync();
        return folder?.Path;
#elif MACCATALYST
        // macOS: Use MAUI FolderPicker or fallback
        // MAUI doesn't have a cross-platform FolderPicker, so we use a workaround
        await Task.CompletedTask;
        return null; // macOS folder picking handled in components directly
#else
        await Task.CompletedTask;
        return null;
#endif
    }

    public async Task<PlatformFileResult?> PickFileAsync(string title, IEnumerable<string> allowedExtensions)
    {
        var customFileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
        {
            { DevicePlatform.WinUI, allowedExtensions },
            { DevicePlatform.macOS, allowedExtensions },
            { DevicePlatform.MacCatalyst, allowedExtensions }
        });

        var result = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = title,
            FileTypes = customFileTypes
        });

        if (result == null) return null;
        return new PlatformFileResult(result.FullPath, result.FileName);
    }

    public async Task<string?> SaveFileAsync(string suggestedFileName, string filterName, IEnumerable<string> extensions)
    {
#if WINDOWS
        var savePicker = new Windows.Storage.Pickers.FileSavePicker();
        var window = Application.Current?.Windows[0]?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
        if (window != null)
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);
        }
        savePicker.SuggestedFileName = suggestedFileName;
        savePicker.FileTypeChoices.Add(filterName, extensions.ToList());
        var file = await savePicker.PickSaveFileAsync();
        return file?.Path;
#elif MACCATALYST
        // macOS: Save to Downloads as fallback
        var downloadsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads",
            suggestedFileName + extensions.FirstOrDefault());
        await Task.CompletedTask;
        return downloadsPath;
#else
        await Task.CompletedTask;
        return null;
#endif
    }

    public void OpenInFileManager(string path)
    {
#if WINDOWS
        Process.Start("explorer.exe", $"/select,\"{path}\"");
#elif MACCATALYST
        Process.Start("open", Path.GetDirectoryName(path) ?? path);
#endif
    }

    public async Task<Stream> OpenAppResourceAsync(string relativePath)
    {
        return await FileSystem.OpenAppPackageFileAsync(relativePath);
    }
}
