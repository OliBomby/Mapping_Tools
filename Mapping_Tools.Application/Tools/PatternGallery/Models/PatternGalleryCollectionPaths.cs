namespace Mapping_Tools.Application.Tools.PatternGallery.Models;

/// <summary>Describes the physical folders owned by one Pattern Gallery collection.</summary>
public sealed record PatternGalleryCollectionPaths(
    string Root,
    string Collection,
    string PatternFiles,
    string ProjectFile);

