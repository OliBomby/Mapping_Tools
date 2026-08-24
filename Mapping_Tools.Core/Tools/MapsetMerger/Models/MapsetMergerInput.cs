namespace Mapping_Tools.Core.Tools.MapsetMerger.Models;

/// <summary>
///     Identifies one source mapset and the name used for its exported assets.
/// </summary>
public sealed class MapsetMergerInput
{
    /// <summary>Creates a mapset input.</summary>
    /// <param name="name">The output folder and reference prefix.</param>
    /// <param name="path">The source mapset directory.</param>
    public MapsetMergerInput(string name, string path)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Path = path ?? throw new ArgumentNullException(nameof(path));
    }

    /// <summary>Gets or sets the output-safe mapset name.</summary>
    public string Name { get; set; }

    /// <summary>Gets or sets the source mapset directory.</summary>
    public string Path { get; set; }
}

