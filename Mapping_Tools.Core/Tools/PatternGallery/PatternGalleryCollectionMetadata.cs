using Mapping_Tools.Core.MathUtil;
using Newtonsoft.Json;

namespace Mapping_Tools.Core.Tools.PatternGallery;

/// <summary>
///     Persists the collection-folder identity needed to resolve pattern files.
///     Filesystem operations are implemented by Infrastructure.
/// </summary>
public sealed class PatternGalleryCollectionMetadata
{
    /// <summary>Gets or sets the collection's fixed pattern-files directory name.</summary>
    public string PatternFilesFolderName { get; set; } = "Pattern Files";

    /// <summary>Gets or sets the unique directory name for this collection.</summary>
    public string CollectionFolderName { get; set; } = RNG.RandomString(20);

    /// <summary>
    ///     Gets or sets the configured root used only by compatibility callers;
    ///     this property is never persisted in a project document.
    /// </summary>
    [JsonIgnore]
    public string BasePath { get; set; } = string.Empty;
}
