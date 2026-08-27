using Mapping_Tools.Application.Tools.MetadataManager;

namespace Mapping_Tools.Desktop.Tools.MetadataManager.Models;

/// <summary>Stores the persisted Metadata Manager project.</summary>
public sealed class MetadataManagerProject : MetadataManagerServiceOptions
{
    /// <summary>Gets or sets the optional beatmap used to import metadata.</summary>
    public string ImportPath { get; set; } = string.Empty;
}