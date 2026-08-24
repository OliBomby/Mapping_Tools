namespace Mapping_Tools.Application.Settings.Models;

/// <summary>
///     Captures a window's normal-state position and size in device-independent pixels.
/// </summary>
/// <param name="X">The horizontal position of the left edge.</param>
/// <param name="Y">The vertical position of the top edge.</param>
/// <param name="Width">The window width.</param>
/// <param name="Height">The window height.</param>
public sealed record WindowBounds(double X, double Y, double Width, double Height);

