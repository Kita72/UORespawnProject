using System.Text.Json;
using System.Text.Json.Serialization;
using UORespawnApp.Scripts.Utilities;

namespace UORespawnApp.Scripts.Services
{
    /// <summary>
    /// Service for checking GitHub Releases for application updates.
    /// Compares current version with latest release and provides download link.
    /// </summary>
    public class UpdateChecker
    {
        private static readonly HttpClient _httpClient = new();
        private const string GITHUB_API_URL = "https://api.github.com/repos/Kita72/UORespawnProject/releases/latest";
        private const string CURRENT_VERSION = "2.0.0.1";
        
        // Cached JsonSerializerOptions for deserialization (reused across all calls)
        private static readonly JsonSerializerOptions _jsonOptions = new() 
        { 
            PropertyNameCaseInsensitive = true 
        };
        
        // Cache results to avoid excessive API calls
        private DateTime _lastCheckTime = DateTime.MinValue;
        private UpdateInfo? _cachedUpdateInfo = null;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromHours(1);
        
        public string CurrentVersion => CURRENT_VERSION;
        
        /// <summary>
        /// Checks GitHub API for the latest release version.
        /// Results are cached for 1 hour to avoid rate limiting.
        /// </summary>
        public async Task<UpdateInfo> CheckForUpdateAsync()
        {
            // Return cached result if still valid
            if (_cachedUpdateInfo != null && DateTime.Now - _lastCheckTime < _cacheExpiration)
            {
                Logger.Info("Returning cached update check result");
                return _cachedUpdateInfo;
            }
            
            try
            {
                Logger.Info($"Checking for updates... Current version: {CURRENT_VERSION}");
                
                // Set user agent (GitHub API requires it)
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "UORespawn-UpdateChecker");
                _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
                
                // Call GitHub API
                var response = await _httpClient.GetAsync(GITHUB_API_URL);
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                var release = JsonSerializer.Deserialize<GitHubRelease>(json, _jsonOptions);
                
                if (release?.TagName == null)
                {
                    Logger.Warning("GitHub API returned no release data");
                    return UpdateChecker.CreateNoUpdateInfo("Unable to check for updates");
                }
                
                // Parse versions (strip 'v' prefix if present)
                var latestVersionString = release.TagName.TrimStart('v');
                
                Logger.Info($"Latest release on GitHub: {latestVersionString}");
                
                // Compare versions
                var current = new Version(CURRENT_VERSION);
                var latest = new Version(latestVersionString);
                
                var updateInfo = new UpdateInfo
                {
                    CurrentVersion = CURRENT_VERSION,
                    LatestVersion = latestVersionString,
                    HasUpdate = latest > current,
                    ReleaseUrl = release.HtmlUrl ?? "https://github.com/Kita72/UORespawnProject/releases",
                    ReleaseNotes = release.Body,
                    ReleasedAt = release.PublishedAt,
                    CheckedAt = DateTime.Now
                };
                
                if (updateInfo.HasUpdate)
                {
                    Logger.Info($"Update available: {CURRENT_VERSION} → {latestVersionString}");
                }
                else
                {
                    Logger.Info("Application is up to date");
                }
                
                // Cache the result
                _cachedUpdateInfo = updateInfo;
                _lastCheckTime = DateTime.Now;
                
                return updateInfo;
            }
            catch (HttpRequestException ex)
            {
                Logger.Warning($"Network error checking for updates: {ex.Message}");
                return UpdateChecker.CreateNoUpdateInfo("Network error - check internet connection");
            }
            catch (Exception ex)
            {
                Logger.Error("Error checking for updates", ex);
                return UpdateChecker.CreateNoUpdateInfo($"Error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Clears the update check cache, forcing a fresh check on next call.
        /// </summary>
        public void ClearCache()
        {
            _cachedUpdateInfo = null;
            _lastCheckTime = DateTime.MinValue;
            Logger.Info("Update check cache cleared");
        }
        
        private static UpdateInfo CreateNoUpdateInfo(string error)
        {
            return new UpdateInfo
            {
                CurrentVersion = CURRENT_VERSION,
                LatestVersion = CURRENT_VERSION,
                HasUpdate = false,
                ReleaseUrl = "https://github.com/Kita72/UORespawnProject/releases",
                ReleaseNotes = null,
                ReleasedAt = null,
                CheckedAt = DateTime.Now,
                ErrorMessage = error
            };
        }
    }
    
    /// <summary>
    /// Information about available updates
    /// </summary>
    public class UpdateInfo
    {
        public string CurrentVersion { get; set; } = "";
        public string LatestVersion { get; set; } = "";
        public bool HasUpdate { get; set; }
        public string ReleaseUrl { get; set; } = "";
        public string? ReleaseNotes { get; set; }
        public DateTime? ReleasedAt { get; set; }
        public DateTime CheckedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }
    
    /// <summary>
    /// GitHub Release API response model
    /// </summary>
    internal class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; set; }
        
        [JsonPropertyName("html_url")]
        public string? HtmlUrl { get; set; }
        
        [JsonPropertyName("body")]
        public string? Body { get; set; }
        
        [JsonPropertyName("published_at")]
        public DateTime? PublishedAt { get; set; }
    }
}
