using Mapping_Tools.Core.Tools.RhythmGuide.Models;

namespace Mapping_Tools.Application.Tools.RhythmGuide;

/// <summary>Represents the complete legacy-compatible Rhythm Guide project document.</summary>
public sealed class RhythmGuideProject
{
    /// <summary>Gets or sets the generator and application options stored by the project.</summary>
    public RhythmGuideProjectOptions GuideGeneratorArgs { get; set; } = new();

    /// <summary>
    ///     Contains the Core generator inputs together with the paths and export
    ///     choices owned by the application layer.
    /// </summary>
    public sealed class RhythmGuideProjectOptions : RhythmGuideOptions
    {
        /// <summary>Gets or sets the source beatmap paths whose rhythm is copied.</summary>
        public string[] Paths { get; set; } = [];

        /// <summary>Gets or sets whether generation creates a map or appends to a target.</summary>
        public RhythmGuideExportMode ExportMode { get; set; } = RhythmGuideExportMode.NewMap;

        /// <summary>Gets or sets the destination beatmap path.</summary>
        public string ExportPath { get; set; } = string.Empty;

    }
}
