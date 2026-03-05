using Microsoft.UI.Xaml;
using System.Threading;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UORespawnApp.WinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : MauiWinUIApplication
    {
        private static Mutex? _mutex;
        private const string MutexName = "UORespawnEditor_SingleInstance_Mutex";

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            // Enforce single instance - prevent data contamination from multiple editors
            _mutex = new Mutex(true, MutexName, out bool createdNew);

            if (!createdNew)
            {
                // Another instance is already running
                var dialog = new Windows.UI.Popups.MessageDialog(
                    "UORespawn Editor is already running.\n\nOnly one instance can run at a time to prevent data conflicts with the server.",
                    "Instance Already Running");

                // Show dialog and exit
                _ = dialog.ShowAsync();

                // Give dialog time to display, then exit
                Thread.Sleep(100);
                Environment.Exit(0);
                return;
            }

            this.InitializeComponent();
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
