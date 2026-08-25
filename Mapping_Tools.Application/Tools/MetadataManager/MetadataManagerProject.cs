using Mapping_Tools.Core.Tools.MetadataManager;

namespace Mapping_Tools.Application.Tools.MetadataManager;

/// <summary>
///     Represents the complete Metadata Manager project persisted by the shell.
/// </summary>
/// <remarks>
///     The direct property layout intentionally matches the legacy
///     <c>MetadataManagerVm</c> JSON document.
/// </remarks>
public sealed class MetadataManagerProject : MetadataManagerOptions
{
    /// <summary>Gets or sets the optional beatmap used to import metadata.</summary>
    public string ImportPath { get; set; } = string.Empty;

    /// <summary>Gets or sets the vertical-bar-separated target beatmap paths.</summary>
    public string ExportPath { get; set; } = string.Empty;

}
