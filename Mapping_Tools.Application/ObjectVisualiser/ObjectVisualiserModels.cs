using Mapping_Tools.Core.Classes.MathUtil;
using Mapping_Tools.Core.Classes.ObjectVisualiser;

namespace Mapping_Tools.Application.ObjectVisualiser;

/// <summary>Identifies the gameplay shape represented by a visualizer object.</summary>
public enum ObjectVisualiserObjectKind
{
    /// <summary>A single hit circle.</summary>
    Circle,

    /// <summary>A slider polyline with anchors and optional progress.</summary>
    Slider,

    /// <summary>A spinner centered on the osu! playfield.</summary>
    Spinner
}

/// <summary>Describes which part of a visualizer object was hit.</summary>
public enum ObjectVisualiserHitPart
{
    /// <summary>No object was hit.</summary>
    None,

    /// <summary>The main circle, slider stroke, or spinner ring was hit.</summary>
    Body,

    /// <summary>A slider control anchor was hit.</summary>
    Anchor
}

/// <summary>Immutable visual-editor data for one hit object.</summary>
public sealed class ObjectVisualiserObject
{
    /// <summary>Creates an object description without framework rendering types.</summary>
    /// <param name="id">The stable identifier used by selection and hover state.</param>
    /// <param name="kind">The gameplay shape.</param>
    /// <param name="position">The object start position in osu! coordinates.</param>
    /// <param name="radius">The object's world-space visual radius.</param>
    /// <param name="path">The slider path, when the object has one.</param>
    /// <param name="anchors">The slider anchors in osu! coordinates.</param>
    /// <param name="endPosition">The final slider position, or the start position for other objects.</param>
    /// <param name="comboIndex">The zero-based combo index.</param>
    /// <param name="startsCombo">Whether this object starts a combo.</param>
    public ObjectVisualiserObject(
        int id,
        ObjectVisualiserObjectKind kind,
        Vector2 position,
        double radius,
        ObjectVisualiserPath? path = null,
        IEnumerable<Vector2>? anchors = null,
        Vector2? endPosition = null,
        int comboIndex = 0,
        bool startsCombo = false)
    {
        if (!double.IsFinite(radius) || radius < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(radius));
        }

        if (!double.IsFinite(position.X) || !double.IsFinite(position.Y))
        {
            throw new ArgumentOutOfRangeException(nameof(position));
        }

        Vector2 resolvedEndPosition = endPosition ?? position;
        if (!double.IsFinite(resolvedEndPosition.X) || !double.IsFinite(resolvedEndPosition.Y))
        {
            throw new ArgumentOutOfRangeException(nameof(endPosition));
        }

        Vector2[] anchorArray = (anchors ?? []).ToArray();
        if (anchorArray.Any(anchor => !double.IsFinite(anchor.X) || !double.IsFinite(anchor.Y)))
        {
            throw new ArgumentException("Visualizer anchors must be finite.", nameof(anchors));
        }

        Id = id;
        Kind = kind;
        Position = position;
        Radius = radius;
        Path = path;
        Anchors = Array.AsReadOnly(anchorArray);
        EndPosition = resolvedEndPosition;
        ComboIndex = comboIndex;
        StartsCombo = startsCombo;
    }

    /// <summary>Gets the stable object identifier.</summary>
    public int Id { get; }

    /// <summary>Gets the gameplay shape.</summary>
    public ObjectVisualiserObjectKind Kind { get; }

    /// <summary>Gets the start position in osu! coordinates.</summary>
    public Vector2 Position { get; }

    /// <summary>Gets the world-space visual radius used for shape and hit testing.</summary>
    public double Radius { get; }

    /// <summary>Gets the slider polyline, or <see langword="null"/> for non-sliders or unusable paths.</summary>
    public ObjectVisualiserPath? Path { get; }

    /// <summary>Gets the slider control anchors in drawing order.</summary>
    public IReadOnlyList<Vector2> Anchors { get; }

    /// <summary>Gets the final slider position.</summary>
    public Vector2 EndPosition { get; }

    /// <summary>Gets the zero-based combo index.</summary>
    public int ComboIndex { get; }

    /// <summary>Gets whether this object starts a combo.</summary>
    public bool StartsCombo { get; }
}

/// <summary>Contains immutable visualizer objects and their aggregate world bounds.</summary>
public sealed class ObjectVisualiserScene
{
    /// <summary>Creates a scene and computes its content bounds.</summary>
    /// <param name="objects">The objects to draw in back-to-front order.</param>
    public ObjectVisualiserScene(IEnumerable<ObjectVisualiserObject> objects)
    {
        ArgumentNullException.ThrowIfNull(objects);
        ObjectVisualiserObject[] objectArray = objects.ToArray();
        Objects = Array.AsReadOnly(objectArray);

        ObjectVisualiserBounds bounds = ObjectVisualiserBounds.Empty;
        var hasBounds = false;
        foreach (ObjectVisualiserObject visualObject in objectArray)
        {
            ObjectVisualiserBounds objectBounds = visualObject.Kind == ObjectVisualiserObjectKind.Slider && visualObject.Path is not null
                ? visualObject.Path.Bounds.Inflate(visualObject.Radius, visualObject.Radius)
                :
                new ObjectVisualiserBounds(visualObject.Position.X - visualObject.Radius,
                    visualObject.Position.Y - visualObject.Radius,
                    visualObject.Radius * 2,
                    visualObject.Radius * 2);
            if (!hasBounds)
            {
                bounds = objectBounds;
                hasBounds = true;
            }
            else
            {
                bounds = bounds.Union(objectBounds);
            }
        }

        ContentBounds = hasBounds ? bounds : ObjectVisualiserBounds.Empty;
    }

    /// <summary>Gets the objects in draw order.</summary>
    public IReadOnlyList<ObjectVisualiserObject> Objects { get; }

    /// <summary>Gets the smallest bounds containing all objects.</summary>
    public ObjectVisualiserBounds ContentBounds { get; }

    /// <summary>Finds an object by stable identifier.</summary>
    /// <param name="id">The identifier to look up.</param>
    /// <returns>The matching object, or <see langword="null"/>.</returns>
    public ObjectVisualiserObject? Find(int id) => Objects.FirstOrDefault(item => item.Id == id);
}

/// <summary>Describes an object and sub-part returned by visualizer hit testing.</summary>
public sealed class ObjectVisualiserHit
{
    /// <summary>Creates a hit result.</summary>
    /// <param name="visualObject">The hit scene object.</param>
    /// <param name="part">The hit object part.</param>
    /// <param name="anchorIndex">The anchor index, or negative when the part is not an anchor.</param>
    public ObjectVisualiserHit(ObjectVisualiserObject visualObject, ObjectVisualiserHitPart part, int anchorIndex = -1)
    {
        ArgumentNullException.ThrowIfNull(visualObject);
        Object = visualObject;
        Part = part;
        AnchorIndex = anchorIndex;
    }

    /// <summary>Gets the hit scene object.</summary>
    public ObjectVisualiserObject Object { get; }

    /// <summary>Gets the hit object part.</summary>
    public ObjectVisualiserHitPart Part { get; }

    /// <summary>Gets the hit anchor index, or negative when the part is not an anchor.</summary>
    public int AnchorIndex { get; }
}
