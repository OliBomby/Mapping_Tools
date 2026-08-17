using Avalonia.Media;

namespace Mapping_Tools.Desktop.Controls;

/// <summary>Describes one square marker drawn at normalized slider progress.</summary>
public sealed class ObjectVisualiserMarker
{
    /// <summary>Creates a marker with an optional brush.</summary>
    /// <param name="progress">The normalized slider progress from zero to one.</param>
    /// <param name="size">The marker square size in world coordinates.</param>
    /// <param name="brush">The marker fill brush.</param>
    public ObjectVisualiserMarker(double progress, double size, IBrush? brush)
    {
        Progress = progress;
        Size = size;
        Brush = brush;
    }

    /// <summary>Gets the normalized slider progress.</summary>
    public double Progress { get; }

    /// <summary>Gets the marker square size in world coordinates.</summary>
    public double Size { get; }

    /// <summary>Gets the marker fill brush.</summary>
    public IBrush? Brush { get; }
}
