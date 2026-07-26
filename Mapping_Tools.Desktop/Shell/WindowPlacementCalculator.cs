using Mapping_Tools.Application.Settings;

namespace Mapping_Tools.Desktop.Shell;

/// <summary>
/// Describes a connected monitor's usable area in device-independent pixels.
/// </summary>
/// <param name="X">Left edge.</param>
/// <param name="Y">Top edge.</param>
/// <param name="Width">Usable width.</param>
/// <param name="Height">Usable height.</param>
/// <param name="IsPrimary">Whether this is the primary monitor.</param>
public sealed record DesktopWorkingArea(
    double X,
    double Y,
    double Width,
    double Height,
    bool IsPrimary);

/// <summary>
/// Produces visible, usable restore bounds after monitor changes.
/// </summary>
public static class WindowPlacementCalculator
{
    /// <summary>
    /// Clamps persisted bounds to the intersecting monitor, or the primary monitor
    /// when the original monitor is no longer connected.
    /// </summary>
    /// <param name="saved">Persisted device-independent bounds.</param>
    /// <param name="screens">Connected monitor working areas.</param>
    /// <param name="defaultBounds">Fallback size and position.</param>
    /// <returns>Safe normal-state bounds.</returns>
    public static WindowBounds Restore(
        WindowBounds? saved,
        IReadOnlyList<DesktopWorkingArea> screens,
        WindowBounds defaultBounds)
    {
        ArgumentNullException.ThrowIfNull(screens);
        if (screens.Count == 0)
        {
            return defaultBounds;
        }

        WindowBounds candidate = IsUsable(saved) ? saved! : defaultBounds;
        DesktopWorkingArea screen = screens
            .OrderByDescending(area => IntersectionArea(candidate, area))
            .First();
        if (IntersectionArea(candidate, screen) <= 0)
        {
            screen = screens.FirstOrDefault(area => area.IsPrimary) ?? screens[0];
        }

        double width = Math.Clamp(candidate.Width, 500, Math.Max(500, screen.Width));
        double height = Math.Clamp(candidate.Height, 200, Math.Max(200, screen.Height));
        double maximumX = Math.Max(screen.X, screen.X + screen.Width - width);
        double maximumY = Math.Max(screen.Y, screen.Y + screen.Height - height);
        double x = Math.Clamp(candidate.X, screen.X, maximumX);
        double y = Math.Clamp(candidate.Y, screen.Y, maximumY);
        return new WindowBounds(x, y, width, height);
    }

    private static bool IsUsable(WindowBounds? bounds) =>
        bounds is { Width: >= 1, Height: >= 1 } &&
        double.IsFinite(bounds.X) &&
        double.IsFinite(bounds.Y) &&
        double.IsFinite(bounds.Width) &&
        double.IsFinite(bounds.Height);

    private static double IntersectionArea(WindowBounds bounds, DesktopWorkingArea screen)
    {
        double width = Math.Max(
            0,
            Math.Min(bounds.X + bounds.Width, screen.X + screen.Width) -
            Math.Max(bounds.X, screen.X));
        double height = Math.Max(
            0,
            Math.Min(bounds.Y + bounds.Height, screen.Y + screen.Height) -
            Math.Max(bounds.Y, screen.Y));
        return width * height;
    }
}
