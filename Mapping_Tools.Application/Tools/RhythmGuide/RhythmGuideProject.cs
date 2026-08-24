using Mapping_Tools.Core.Tools.RhythmGuide;
using Mapping_Tools.Core.Tools.RhythmGuide.Models;

namespace Mapping_Tools.Application.Tools.RhythmGuide;

/// <summary>Represents the complete legacy-compatible Rhythm Guide project document.</summary>
public sealed class RhythmGuideProject
{
    /// <summary>Gets or sets the generator options stored by the project.</summary>
    public RhythmGuideOptions GuideGeneratorArgs { get; set; } = new();
}

