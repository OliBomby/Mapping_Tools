using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.SnappingTools;
using Mapping_Tools.Core.Tools.SnappingTools.Serialization;

namespace Mapping_Tools.Application.Tools.GeometryDashboard;

/// <summary>
///     Carries the validated editor state needed by Geometry Dashboard without
///     exposing Editor Reader's vendor-specific memory model.
/// </summary>
public sealed class GeometryDashboardEditorSnapshot
{
    /// <summary>
    ///     Creates a snapshot of one successful editor-memory read.
    /// </summary>
    /// <param name="path">The full path reconstructed from the configured Songs directory.</param>
    /// <param name="approachRate">The live osu! approach-rate value.</param>
    /// <param name="circleSize">The live osu! circle-size value.</param>
    /// <param name="editorTime">The editor playhead in milliseconds.</param>
    /// <param name="hitObjects">The complete live object list without editor-only selection state.</param>
    /// <param name="selectedHitObjects">The separate live editor selection snapshot.</param>
    public GeometryDashboardEditorSnapshot(
        string path,
        double approachRate,
        double circleSize,
        int editorTime,
        IReadOnlyList<HitObject> hitObjects,
        IReadOnlyList<HitObject>? selectedHitObjects = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(hitObjects);

        Path = path;
        ApproachRate = approachRate;
        CircleSize = circleSize;
        EditorTime = editorTime;
        HitObjects = hitObjects.ToArray();
        SelectedHitObjects = (selectedHitObjects ?? []).ToArray();
    }

    /// <summary>Gets the full path of the beatmap currently held by the editor.</summary>
    public string Path { get; }

    /// <summary>Gets the live approach-rate value used for visibility calculations.</summary>
    public double ApproachRate { get; }

    /// <summary>Gets the live circle-size value used for hit-object radius calculations.</summary>
    public double CircleSize { get; }

    /// <summary>Gets the live editor playhead in milliseconds.</summary>
    public int EditorTime { get; }

    /// <summary>Gets the complete live hit-object list without selection state.</summary>
    public IReadOnlyList<HitObject> HitObjects { get; }

    /// <summary>Gets the separate live editor selection snapshot.</summary>
    public IReadOnlyList<HitObject> SelectedHitObjects { get; }
}

