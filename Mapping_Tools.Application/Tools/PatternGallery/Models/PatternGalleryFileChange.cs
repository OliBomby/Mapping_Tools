namespace Mapping_Tools.Application.Tools.PatternGallery.Models;

/// <summary>Stores the bytes of one collection file before and after an edit.</summary>
/// <param name="RelativePath">The file path relative to its collection directory.</param>
/// <param name="Before">The original bytes, or null when the file did not exist.</param>
/// <param name="After">The resulting bytes, or null when the file was removed.</param>
public sealed record PatternGalleryFileChange(string RelativePath, byte[]? Before, byte[]? After);
