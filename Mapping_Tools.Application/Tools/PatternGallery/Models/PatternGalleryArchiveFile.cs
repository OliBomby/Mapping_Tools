namespace Mapping_Tools.Application.Tools.PatternGallery.Models;

/// <summary>One pattern file included in a collection archive.</summary>
public sealed record PatternGalleryArchiveFile(string FileName, byte[] Content);

