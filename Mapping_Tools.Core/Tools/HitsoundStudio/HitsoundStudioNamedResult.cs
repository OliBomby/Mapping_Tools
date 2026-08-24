using Mapping_Tools.Core.Audio;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.HitsoundStuff;
using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Core.Tools.HitsoundStudio;

/// <summary>Contains named events, source names, and positions for non-standard export.</summary>
public sealed class HitsoundStudioNamedResult
{
    /// <summary>Creates a named-mode result.</summary>
    /// <param name="events">The generated events.</param>
    /// <param name="schema">The persisted singleton-name schema.</param>
    /// <param name="names">The source-to-name map.</param>
    /// <param name="positions">The source-to-position map.</param>
    public HitsoundStudioNamedResult(
        IReadOnlyList<HitsoundEvent> events,
        SampleSchema schema,
        IReadOnlyDictionary<SampleGeneratingArgs, string> names,
        IReadOnlyDictionary<SampleGeneratingArgs, Vector2> positions)
    {
        Events = events;
        Schema = schema;
        Names = names;
        Positions = positions;
    }

    /// <summary>Gets the generated events.</summary>
    public IReadOnlyList<HitsoundEvent> Events { get; }

    /// <summary>Gets the persisted source-name schema.</summary>
    public SampleSchema Schema { get; }

    /// <summary>Gets the names assigned to source specifications.</summary>
    public IReadOnlyDictionary<SampleGeneratingArgs, string> Names { get; }

    /// <summary>Gets the deterministic positions assigned to source specifications.</summary>
    public IReadOnlyDictionary<SampleGeneratingArgs, Vector2> Positions { get; }
}
