using Mapping_Tools.Core.Classes.MathUtil;
using Mapping_Tools.Core.Classes.ObjectVisualiser;

namespace Mapping_Tools.Application.ObjectVisualiser;

/// <summary>Performs deterministic object and anchor hit testing outside any UI framework.</summary>
public static class ObjectVisualiserHitTester
{
    /// <summary>Matches the legacy control's maximum number of visible slider anchors.</summary>
    public const int MaxAnchorCount = 1500;

    /// <summary>Gets the radius of the small center ring drawn for a spinner.</summary>
    public const double SpinnerCenterRadius = 5;

    /// <summary>Finds the front-most object or visible anchor at a viewport point.</summary>
    /// <param name="scene">The scene to inspect.</param>
    /// <param name="transform">The world-to-viewport transform currently in use.</param>
    /// <param name="viewportPoint">The input point in viewport pixels.</param>
    /// <param name="hitTolerance">The viewport hit tolerance in pixels.</param>
    /// <param name="showAnchors">Whether slider anchors participate in hit testing.</param>
    /// <param name="anchorSize">The world-space anchor square size.</param>
    /// <param name="bodyRadius">The optional world-space circle/slider hit radius. When omitted, each object's radius is used.</param>
    /// <returns>The front-most hit, or <see langword="null"/>.</returns>
    public static ObjectVisualiserHit? HitTest(
        ObjectVisualiserScene scene,
        ObjectVisualiserTransform transform,
        Vector2 viewportPoint,
        double hitTolerance,
        bool showAnchors = false,
        double anchorSize = 0.2,
        double? bodyRadius = null)
    {
        ArgumentNullException.ThrowIfNull(scene);
        if (!double.IsFinite(hitTolerance) || hitTolerance < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(hitTolerance));
        }

        if (!double.IsFinite(anchorSize) || anchorSize < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(anchorSize));
        }

        if (bodyRadius is not null && (!double.IsFinite(bodyRadius.Value) || bodyRadius.Value < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(bodyRadius));
        }

        if (!double.IsFinite(viewportPoint.X) || !double.IsFinite(viewportPoint.Y))
        {
            throw new ArgumentOutOfRangeException(nameof(viewportPoint));
        }

        Vector2 worldPoint = transform.ViewportToWorld(viewportPoint);
        double worldTolerance = hitTolerance / transform.Scale;
        for (var objectIndex = scene.Objects.Count - 1; objectIndex >= 0; objectIndex--)
        {
            ObjectVisualiserObject visualObject = scene.Objects[objectIndex];
            if (showAnchors && visualObject.Kind == ObjectVisualiserObjectKind.Slider &&
                visualObject.Path is not null && visualObject.Anchors.Count <= MaxAnchorCount)
            {
                for (var anchorIndex = visualObject.Anchors.Count - 1; anchorIndex >= 0; anchorIndex--)
                {
                    Vector2 anchor = visualObject.Anchors[anchorIndex];
                    if (Math.Abs(worldPoint.X - anchor.X) <= anchorSize / 2 + worldTolerance &&
                        Math.Abs(worldPoint.Y - anchor.Y) <= anchorSize / 2 + worldTolerance)
                    {
                        return new ObjectVisualiserHit(visualObject, ObjectVisualiserHitPart.Anchor, anchorIndex);
                    }
                }
            }

            bool isHit = visualObject.Kind switch
            {
                ObjectVisualiserObjectKind.Circle => (worldPoint - visualObject.Position).Length <= (bodyRadius ?? visualObject.Radius) + worldTolerance,
                ObjectVisualiserObjectKind.Slider => visualObject.Path is not null &&
                    visualObject.Path.DistanceTo(worldPoint) <= (bodyRadius ?? visualObject.Radius) + worldTolerance,
                ObjectVisualiserObjectKind.Spinner =>
                    Math.Abs((worldPoint - visualObject.Position).Length - visualObject.Radius) <= worldTolerance ||
                    Math.Abs((worldPoint - visualObject.Position).Length - SpinnerCenterRadius) <= worldTolerance,
                _ => false
            };

            if (isHit)
            {
                return new ObjectVisualiserHit(visualObject, ObjectVisualiserHitPart.Body);
            }
        }

        return null;
    }
}
