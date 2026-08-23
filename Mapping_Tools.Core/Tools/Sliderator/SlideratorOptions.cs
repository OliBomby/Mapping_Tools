using Mapping_Tools.Core.Graph;
using Mapping_Tools.Core.MathUtil;
using Newtonsoft.Json;

namespace Mapping_Tools.Core.Tools.Sliderator;

/// <summary>Chooses the source objects imported into Sliderator.</summary>
public enum SlideratorImportMode
{
    /// <summary>Uses the objects selected in the live editor.</summary>
    Selected,

    /// <summary>Uses objects covered by editor bookmarks.</summary>
    Bookmarked,

    /// <summary>Uses objects matched by the time-code expression.</summary>
    Time,
}

/// <summary>Chooses how the generated object is written at the export time.</summary>
public enum SlideratorExportMode
{
    /// <summary>Adds the generated object beside any object already at the time.</summary>
    Add,

    /// <summary>Replaces the object already at the time.</summary>
    Override,
}

/// <summary>Chooses whether graph values represent position or velocity.</summary>
public enum SlideratorGraphMode
{
    /// <summary>Interprets graph values as normalized slider position.</summary>
    Position,

    /// <summary>Interprets graph values as slider velocity in SV units.</summary>
    Velocity,
}

/// <summary>
///     Contains the framework-neutral Sliderator inputs used by the Core engine.
///     UI-only selection, progress, and editor state remain outside this assembly.
/// </summary>
public class SlideratorOptions
{
    /// <summary>Gets or sets the imported object selection mode.</summary>
    public SlideratorImportMode ImportModeSetting { get; set; } = SlideratorImportMode.Selected;

    /// <summary>Gets or sets the time-code expression used by time selection.</summary>
    public string TimeCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the map's global slider multiplier.</summary>
    public double GlobalSv { get; set; } = 1.4;

    /// <summary>Gets or sets the requested graph duration in beats.</summary>
    [JsonIgnore]
    public double GraphBeats { get; set; } = 3;

    /// <summary>Gets or sets the redline BPM applied to the graph duration.</summary>
    [JsonIgnore]
    public double BeatsPerMinute { get; set; } = 180;

    /// <summary>Gets or sets the source slider's effective pixel length.</summary>
    [JsonIgnore]
    public double PixelLength { get; set; } = 100;

    /// <summary>Gets or sets the timeline divisor used for stream output.</summary>
    public int BeatSnapDivisor { get; set; } = 4;

    /// <summary>Gets or sets the export timestamp in milliseconds.</summary>
    public double ExportTime { get; set; }

    /// <summary>Gets or sets whether export adds or replaces an object.</summary>
    public SlideratorExportMode ExportModeSetting { get; set; } = SlideratorExportMode.Add;

    /// <summary>Gets or sets whether the graph is evaluated as position or velocity.</summary>
    public SlideratorGraphMode GraphModeSetting { get; set; } = SlideratorGraphMode.Position;

    /// <summary>Gets or sets the maximum allowed SV value for normal output.</summary>
    public double VelocityLimit { get; set; } = 10;

    /// <summary>Gets or sets whether red slider-path anchors are shown in the preview.</summary>
    public bool ShowRedAnchors { get; set; }

    /// <summary>Gets or sets whether graph anchors are shown in the preview.</summary>
    public bool ShowGraphAnchors { get; set; }

    /// <summary>Gets or sets whether the manually entered velocity is used.</summary>
    public bool ManualVelocity { get; set; }

    /// <summary>Gets or sets the manually entered velocity in SV units.</summary>
    public double NewVelocity { get; set; } = 1;

    /// <summary>Gets or sets the minimum dendrite length used by slideration.</summary>
    public double MinDendrite { get; set; } = 2;

    /// <summary>Gets or sets the distance travelled by the current graph.</summary>
    public double DistanceTraveled { get; set; }

    /// <summary>Gets or sets whether SV is delegated to BPM timing points.</summary>
    public bool DelegateToBpm { get; set; }

    /// <summary>Gets or sets whether slider ticks are removed from delegated output.</summary>
    public bool RemoveSliderTicks { get; set; }

    /// <summary>Gets or sets whether the result is written as a normal slider.</summary>
    public bool ExportAsNormal { get; set; } = true;

    /// <summary>Gets or sets whether the result is written as a stream of circles.</summary>
    public bool ExportAsStream { get; set; }

    /// <summary>Gets or sets whether the result is written as an invisible slider.</summary>
    public bool ExportAsInvisibleSlider { get; set; }

    /// <summary>Gets or sets the graph state evaluated by the engine.</summary>
    public GraphState GraphState { get; set; } = CreateDefaultGraph();

    /// <summary>Creates Sliderator's diagonal position graph with the requested bounds.</summary>
    /// <param name="graphBeats">The graph duration in beats.</param>
    /// <returns>A graph beginning at zero and ending at one.</returns>
    public static GraphState CreatePositionGraph(double graphBeats)
    {
        return new GraphState(
            [
                new GraphAnchor(new Vector2(0, 0)),
                new GraphAnchor(new Vector2((float)graphBeats, 1)),
            ],
            0,
            0,
            graphBeats,
            1);
    }

    private static GraphState CreateDefaultGraph()
    {
        return CreatePositionGraph(3);
    }
}
