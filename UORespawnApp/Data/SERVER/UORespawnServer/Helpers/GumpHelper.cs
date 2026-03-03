namespace Server.Custom.UORespawnServer.Helpers
{
    internal static class GumpHelper
    {
        // Helper methods for HTML formatting
        internal static string Bold(string text) => $"<B>{text}</B>";
        internal static string Center(string text) => $"<CENTER>{text}</CENTER>";
        internal static string BoldCenter(string text) => Bold(Center(text));
    }
}
