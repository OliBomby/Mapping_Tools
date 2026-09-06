namespace Mapping_Tools.Desktop.Tools.PatternGallery.Interactions;

/// <summary>Carries the accepted collection and directory names from the rename form.</summary>
public sealed record PatternGalleryCollectionRenameInput(
    string NewName,
    string NewFolderName);
