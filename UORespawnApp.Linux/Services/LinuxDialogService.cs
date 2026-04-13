using System.Diagnostics;
using UORespawnApp.Scripts.Services.Platform;

namespace UORespawnApp;

/// <summary>
/// Linux implementation of IPlatformDialogService.
/// Uses zenity (GTK) or kdialog (KDE) for native file/folder dialogs.
/// Falls back to console path input if neither is available.
/// </summary>
public class LinuxDialogService : IPlatformDialogService
{
    private static readonly Lazy<string?> DialogTool = new(() => DetectDialogTool());

    public async Task<string?> PickFolderAsync(string? suggestedStartLocation = null)
    {
        var tool = DialogTool.Value;

        if (tool == "zenity")
        {
            var args = "--file-selection --directory --title=\"Select Folder\"";
            if (!string.IsNullOrEmpty(suggestedStartLocation))
                args += $" --filename=\"{suggestedStartLocation}/\"";
            return await RunDialogCommand("zenity", args);
        }
        else if (tool == "kdialog")
        {
            var args = "--getexistingdirectory";
            if (!string.IsNullOrEmpty(suggestedStartLocation))
                args += $" \"{suggestedStartLocation}\"";
            else
                args += " .";
            args += " --title \"Select Folder\"";
            return await RunDialogCommand("kdialog", args);
        }

        // Fallback: no dialog tool available
        Console.Error.WriteLine("Warning: No folder picker available (install zenity or kdialog)");
        return null;
    }

    public async Task<PlatformFileResult?> PickFileAsync(string title, IEnumerable<string> allowedExtensions)
    {
        var tool = DialogTool.Value;
        var extList = allowedExtensions.ToList();

        if (tool == "zenity")
        {
            var filter = BuildZenityFilter(extList);
            var args = $"--file-selection --title=\"{title}\" {filter}";
            var path = await RunDialogCommand("zenity", args);
            if (path != null)
                return new PlatformFileResult(path, Path.GetFileName(path));
        }
        else if (tool == "kdialog")
        {
            var filter = BuildKDialogFilter(extList);
            var args = $"--getopenfilename . \"{filter}\" --title \"{title}\"";
            var path = await RunDialogCommand("kdialog", args);
            if (path != null)
                return new PlatformFileResult(path, Path.GetFileName(path));
        }

        return null;
    }

    public async Task<string?> SaveFileAsync(string suggestedFileName, string filterName, IEnumerable<string> extensions)
    {
        var tool = DialogTool.Value;
        var extList = extensions.ToList();

        if (tool == "zenity")
        {
            var filter = BuildZenityFilter(extList);
            var args = $"--file-selection --save --confirm-overwrite --title=\"Save File\" --filename=\"{suggestedFileName}{extList.FirstOrDefault()}\" {filter}";
            return await RunDialogCommand("zenity", args);
        }
        else if (tool == "kdialog")
        {
            var filter = BuildKDialogFilter(extList);
            var downloadsDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            var args = $"--getsavefilename \"{Path.Combine(downloadsDir, suggestedFileName + extList.FirstOrDefault())}\" \"{filter}\" --title \"Save File\"";
            return await RunDialogCommand("kdialog", args);
        }

        // Fallback: save to Downloads
        var fallbackDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        return Path.Combine(fallbackDir, suggestedFileName + extList.FirstOrDefault());
    }

    public void OpenInFileManager(string path)
    {
        try
        {
            var dir = File.Exists(path) ? Path.GetDirectoryName(path) ?? path : path;
            Process.Start(new ProcessStartInfo
            {
                FileName = "xdg-open",
                Arguments = $"\"{dir}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to open file manager: {ex.Message}");
        }
    }

    public Task<Stream> OpenAppResourceAsync(string relativePath)
    {
        // On Linux (Photino), resources are in the app's base directory
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        var fullPath = Path.Combine(basePath, relativePath);

        if (File.Exists(fullPath))
            return Task.FromResult<Stream>(File.OpenRead(fullPath));

        throw new FileNotFoundException($"App resource not found: {fullPath}");
    }

    // ==================== Helper Methods ====================

    private static string? DetectDialogTool()
    {
        // Prefer zenity (GTK, most common on Linux)
        if (IsCommandAvailable("zenity")) return "zenity";
        if (IsCommandAvailable("kdialog")) return "kdialog";
        return null;
    }

    private static bool IsCommandAvailable(string command)
    {
        try
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "which",
                Arguments = command,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            });
            process?.WaitForExit(2000);
            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<string?> RunDialogCommand(string command, string arguments)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                var result = output.Trim();
                return string.IsNullOrEmpty(result) ? null : result;
            }

            return null; // User cancelled or error
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Dialog command failed: {ex.Message}");
            return null;
        }
    }

    private static string BuildZenityFilter(List<string> extensions)
    {
        if (extensions.Count == 0) return "";
        // zenity format: --file-filter="Images | *.png *.jpg"
        var patterns = string.Join(" ", extensions.Select(e => $"*{e}"));
        return $"--file-filter=\"Files | {patterns}\" --file-filter=\"All Files | *\"";
    }

    private static string BuildKDialogFilter(List<string> extensions)
    {
        if (extensions.Count == 0) return "*";
        // kdialog format: "*.png *.jpg|Image Files"
        var patterns = string.Join(" ", extensions.Select(e => $"*{e}"));
        return $"{patterns}|Files";
    }
}
