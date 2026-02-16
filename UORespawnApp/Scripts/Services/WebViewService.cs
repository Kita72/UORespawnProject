using UORespawnApp.Scripts.Utilities;

namespace UORespawnApp.Scripts.Services
{
    /// <summary>
    /// Service to control the external WebView overlay from Blazor components.
    /// Allows showing/hiding an external website within the app.
    /// </summary>
    public class WebViewService
    {
        /// <summary>
        /// Event fired when the WebView visibility should change.
        /// </summary>
        public event Action<bool, string?>? OnWebViewToggle;

        /// <summary>
        /// Gets whether the external WebView is currently visible.
        /// </summary>
        public bool IsWebViewOpen { get; private set; }

        /// <summary>
        /// Shows the external WebView with the specified URL.
        /// </summary>
        /// <param name="url">The URL to load in the WebView.</param>
        public void ShowWebView(string url)
        {
            IsWebViewOpen = true;
            OnWebViewToggle?.Invoke(true, url);
            Logger.Info($"WebView opened: {url}");
        }

        /// <summary>
        /// Hides the external WebView.
        /// </summary>
        public void HideWebView()
        {
            IsWebViewOpen = false;
            OnWebViewToggle?.Invoke(false, null);
            Logger.Info("WebView closed");
        }

        /// <summary>
        /// Toggles the WebView visibility.
        /// </summary>
        /// <param name="url">The URL to load if showing.</param>
        public void ToggleWebView(string url)
        {
            if (IsWebViewOpen)
            {
                HideWebView();
            }
            else
            {
                ShowWebView(url);
            }
        }
    }
}
