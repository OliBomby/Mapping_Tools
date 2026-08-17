using Mapping_Tools.Core.Classes.MathUtil;

namespace Mapping_Tools.Core.Classes.ObjectVisualiser;

/// <summary>Maps osu! playfield coordinates into a clipped visualizer viewport.</summary>
public readonly record struct ObjectVisualiserTransform
{
    /// <summary>Creates an affine scale-and-translation transform.</summary>
    /// <param name="scale">The positive uniform scale.</param>
    /// <param name="offset">The viewport translation in pixels.</param>
    public ObjectVisualiserTransform(double scale, Vector2 offset)
    {
        if (!double.IsFinite(scale) || scale <= 0 || !double.IsFinite(offset.X) || !double.IsFinite(offset.Y))
        {
            throw new ArgumentOutOfRangeException(nameof(scale));
        }

        Scale = scale;
        Offset = offset;
    }

    /// <summary>Gets the identity transform.</summary>
    public static ObjectVisualiserTransform Identity => new(1, Vector2.Zero);

    /// <summary>Gets the uniform world-to-viewport scale.</summary>
    public double Scale { get; }

    /// <summary>Gets the viewport translation in pixels.</summary>
    public Vector2 Offset { get; }

    /// <summary>Creates a transform that fits bounds into a viewport while preserving orientation.</summary>
    /// <param name="bounds">The world-space content bounds.</param>
    /// <param name="viewportSize">The viewport width and height in pixels.</param>
    /// <param name="padding">The viewport padding in pixels.</param>
    /// <returns>A centered fit transform.</returns>
    public static ObjectVisualiserTransform Fit(
        ObjectVisualiserBounds bounds,
        Vector2 viewportSize,
        double padding = 0)
    {
        if (!double.IsFinite(viewportSize.X) || !double.IsFinite(viewportSize.Y) || viewportSize.X < 0 || viewportSize.Y < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(viewportSize));
        }

        if (!double.IsFinite(padding) || padding < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(padding));
        }

        double availableWidth = Math.Max(0, viewportSize.X - padding * 2);
        double availableHeight = Math.Max(0, viewportSize.Y - padding * 2);
        double scale = bounds.Width > 0 && bounds.Height > 0
            ? Math.Min(availableWidth / bounds.Width, availableHeight / bounds.Height)
            : 1;
        if (!double.IsFinite(scale) || scale <= 0)
        {
            scale = 1;
        }

        Vector2 viewportCenter = viewportSize / 2;
        return new ObjectVisualiserTransform(scale, viewportCenter - bounds.Center * scale);
    }

    /// <summary>Converts a playfield coordinate into viewport coordinates without flipping Y.</summary>
    /// <param name="worldPoint">The osu! coordinate.</param>
    /// <returns>The viewport coordinate.</returns>
    public Vector2 WorldToViewport(Vector2 worldPoint) => worldPoint * Scale + Offset;

    /// <summary>Converts a viewport coordinate into osu! playfield coordinates without flipping Y.</summary>
    /// <param name="viewportPoint">The viewport coordinate.</param>
    /// <returns>The playfield coordinate.</returns>
    public Vector2 ViewportToWorld(Vector2 viewportPoint) => (viewportPoint - Offset) / Scale;

    /// <summary>Translates the viewport while leaving world scale unchanged.</summary>
    /// <param name="viewportDelta">The pixel delta to add to the viewport translation.</param>
    /// <returns>The translated transform.</returns>
    public ObjectVisualiserTransform PanBy(Vector2 viewportDelta) =>
        new(Scale, Offset + viewportDelta);

    /// <summary>Zooms around a fixed viewport point.</summary>
    /// <param name="viewportPoint">The point that remains stationary while zooming.</param>
    /// <param name="factor">The positive multiplicative zoom factor.</param>
    /// <returns>The zoomed transform.</returns>
    public ObjectVisualiserTransform ZoomAt(Vector2 viewportPoint, double factor)
    {
        if (!double.IsFinite(factor) || factor <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(factor));
        }

        double newScale = Scale * factor;
        return new ObjectVisualiserTransform(newScale, viewportPoint - ViewportToWorld(viewportPoint) * newScale);
    }
}
