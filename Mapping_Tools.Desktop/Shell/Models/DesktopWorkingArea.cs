namespace Mapping_Tools.Desktop.Shell.Models;

/// <summary>
///     Describes a connected monitor's usable area in device-independent pixels.
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

