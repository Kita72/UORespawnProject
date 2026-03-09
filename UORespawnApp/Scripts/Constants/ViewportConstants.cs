namespace UORespawnApp.Scripts.Constants;

/// <summary>
/// Centralized constants for map viewport and canvas dimensions.
/// Single source of truth for all map editor components.
/// </summary>
public static class ViewportConstants
{
    // ==================== MAIN VIEWPORT ====================

    /// <summary>Width of the main map canvas in pixels</summary>
    public const int ViewportWidth = 800;

    /// <summary>Height of the main map canvas in pixels</summary>
    public const int ViewportHeight = 600;

    /// <summary>Half of viewport width (for centering calculations)</summary>
    public const int ViewportHalfWidth = ViewportWidth / 2;

    /// <summary>Half of viewport height (for centering calculations)</summary>
    public const int ViewportHalfHeight = ViewportHeight / 2;

    // ==================== MINI MAP ====================

    /// <summary>Width of the mini map in pixels</summary>
    public const int MiniMapWidth = 250;

    /// <summary>Height of the mini map in pixels</summary>
    public const int MiniMapHeight = 188;

    // ==================== ZOOM ====================

    /// <summary>Minimum zoom level allowed</summary>
    public const double MinZoom = 0.1;

    /// <summary>Maximum zoom level allowed</summary>
    public const double MaxZoom = 8.0;

    /// <summary>Default/initial zoom level</summary>
    public const double DefaultZoom = 1.0;

    /// <summary>Zoom increment per wheel tick</summary>
    public const double ZoomStep = 0.1;
}
