namespace Mapping_Tools.Desktop.Tools.PatternGallery.Interactions;

/// <summary> Carries submitted source-file import values.</summary>
public sealed record PatternGalleryFileInput(
    string Name,
    string FilePath,
    string Filter,
    double StartTime,
    double EndTime);

