using UORespawnApp.Scripts.Services;

namespace UORespawnApp
{
    /// <summary>
    /// Main page hosting the Blazor WebView for the UORespawn Editor UI.
    /// All application functionality is rendered through Blazor components.
    /// </summary>
    public partial class MainPage : ContentPage
    {
        private readonly WebViewService? _webViewService;
        private WebView? _externalWebView;
        private Button? _closeButton;

        /// <summary>
        /// Initializes the main page and its Blazor WebView.
        /// </summary>
        public MainPage()
        {
            InitializeComponent();

            // Find controls by name after initialization
            _externalWebView = this.FindByName<WebView>("externalWebView");
            _closeButton = this.FindByName<Button>("closeWebViewButton");

            // Get the WebViewService from DI and subscribe to events
            _webViewService = App.Current?.Handler?.MauiContext?.Services.GetService<WebViewService>();
            if (_webViewService != null)
            {
                _webViewService.OnWebViewToggle += OnWebViewToggle;
            }
        }

        private void OnWebViewToggle(bool show, string? url)
        {
            // Must run on UI thread
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (_externalWebView == null) return;

                if (show && !string.IsNullOrEmpty(url))
                {
                    _externalWebView.Source = new UrlWebViewSource { Url = url };
                    _externalWebView.IsVisible = true;
                    if (_closeButton != null)
                    {
                        _closeButton.IsVisible = true;
                    }
                }
                else
                {
                    _externalWebView.IsVisible = false;
                    _externalWebView.Source = null;
                    if (_closeButton != null)
                    {
                        _closeButton.IsVisible = false;
                    }
                }
            });
        }

        private void OnCloseWebViewClicked(object? sender, EventArgs e)
        {
            _webViewService?.HideWebView();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            if (_webViewService != null)
            {
                _webViewService.OnWebViewToggle -= OnWebViewToggle;
            }
        }
    }
}

