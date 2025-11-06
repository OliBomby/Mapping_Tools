using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Mapping_Tools.Core.BeatmapHelper;

/// <summary>
/// Hashable reference to a file in a beatmap set.
/// </summary>
public interface IBeatmapSetFileInfo : IEquatable<IBeatmapSetFileInfo> {
    /// <summary>
    /// The filename of with file extension.
    /// This is a relative path from the root directory.
    /// </summary>
    [NotNull]
    public string Filename { get; }

    /// <summary>
    /// The size of the file in bytes.
    /// </summary>
    public long Size { get; }

    /// <summary>
    /// Gets a stream with the contents of the file.
    /// </summary>
    /// <returns>The stream with the contents of the file.</returns>
    public Stream GetData();
}