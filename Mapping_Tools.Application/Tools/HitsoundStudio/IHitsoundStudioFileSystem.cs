using Mapping_Tools.Application.Audio;
using Mapping_Tools.Core.Audio;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.HitsoundStuff;

namespace Mapping_Tools.Application.Tools.HitsoundStudio;

/// <summary>Restricts filesystem mutations used by Hitsound Studio export.</summary>
public interface IHitsoundStudioFileSystem
{
    /// <summary>Gets whether a file exists.</summary>
    /// <param name="path">The path to inspect.</param>
    bool FileExists(string path);

    /// <summary>Gets whether a directory exists.</summary>
    /// <param name="path">The directory path to inspect.</param>
    bool DirectoryExists(string path);

    /// <summary>Creates a directory and its parents.</summary>
    /// <param name="path">The directory path to create.</param>
    void CreateDirectory(string path);

    /// <summary>Deletes every file directly inside a directory.</summary>
    /// <param name="path">The directory whose direct files are removed.</param>
    void DeleteFiles(string path);

    /// <summary>Copies one file and replaces an existing destination.</summary>
    /// <param name="sourcePath">The existing source file.</param>
    /// <param name="destinationPath">The destination file to replace.</param>
    void CopyFile(string sourcePath, string destinationPath);
}

