using Mapping_Tools.Core.Audio;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.HitsoundStuff;
using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Core.Tools.HitsoundStudio;

/// <summary>Contains standard-mode events and the schema that generated them.</summary>
public sealed class HitsoundStudioStandardResult
{
    /// <summary>Creates a standard-mode result.</summary>
    /// <param name="events">The generated hitsound events.</param>
    /// <param name="schema">The custom-index schema used by those events.</param>
    public HitsoundStudioStandardResult(IReadOnlyList<HitsoundEvent> events, SampleSchema schema)
    {
        Events = events;
        Schema = schema;
    }

    /// <summary>Gets the generated events.</summary>
    public IReadOnlyList<HitsoundEvent> Events { get; }

    /// <summary>Gets the schema used by the events.</summary>
    public SampleSchema Schema { get; }
}

