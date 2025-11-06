using System.Data;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.IO.Editor;
using Mapping_Tools.Core.BeatmapHelper.IO.Encoding;
using Mapping_Tools.Core.BeatmapHelper.IO.Encoding.HitObjects;
using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Core.Tools.PatternGallery;

/// <summary>
/// Pattern file handler that uses the file system.
/// Stores the pattern beatmaps at "BasePath/CollectionFolderName/PatternFilesFolderName".
/// </summary>
public class OsuPatternFileHandler : IOsuPatternFileHandler {
    /// <summary>
    /// The name for the folder in the collection storing all the pattern beatmap files.
    /// Default value is "Pattern Files".
    /// </summary>
    public string PatternFilesFolderName { get; set; } = @"Pattern Files";

    /// <summary>
    /// The path to the base folder all the pattern collections have to be stored in.
    /// </summary>
    [JsonIgnore]
    public string BasePath { get; set; }

    /// <summary>
    /// The name of the folder to store the collection in.
    /// </summary>
    public string CollectionFolderName { get; set; }

    /// <summary>
    /// Creates a new file handler with a random <see cref="CollectionFolderName"/>.
    /// </summary>
    public OsuPatternFileHandler() {
        CollectionFolderName = RNG.RandomString(20);
    }

    /// <summary>
    /// Creates a new file handler with a random <see cref="CollectionFolderName"/> and a specified <see cref="BasePath"/>.
    /// Will attempt to create the collection directory.
    /// </summary>
    public OsuPatternFileHandler(string basePath) {
        CollectionFolderName = RNG.RandomString(20);
        BasePath = basePath;
        EnsureCollectionFolderExists();
    }

    /// <summary>
    /// Creates a new file handler with a specified <see cref="CollectionFolderName"/> and <see cref="BasePath"/>.
    /// Will attempt to create the collection directory.
    /// </summary>
    public OsuPatternFileHandler(string basePath, string collectionFolderName) {
        CollectionFolderName = collectionFolderName;
        BasePath = basePath;
        EnsureCollectionFolderExists();
    }

    /// <summary>
    /// Ensures the directory to store all the pattern beatmap files in exists.
    /// </summary>
    public void EnsureCollectionFolderExists() {
        Directory.CreateDirectory(GetPatternFilesFolderPath());
    }

    /// <summary>
    /// Gets the absolute path to the collection folder.
    /// </summary>
    /// <returns>The absolute path to the collection folder.</returns>
    public string GetCollectionFolderPath() {
        return Path.Combine(BasePath, CollectionFolderName);
    }

    /// <summary>
    /// Gets the absolute path to the pattern beatmaps folder.
    /// </summary>
    /// <returns>The absolute path to the pattern beatmaps folder.</returns>
    public string GetPatternFilesFolderPath() {
        return Path.Combine(BasePath, GetPatternFilesFolderRelativePath());
    }

    /// <summary>
    /// Gets the relative path to the pattern beatmaps folder.
    /// </summary>
    /// <returns>The relative path to the pattern beatmaps folder.</returns>
    public string GetPatternFilesFolderRelativePath() {
        return Path.Combine(CollectionFolderName, PatternFilesFolderName);
    }

    /// <summary>
    /// Gets the absolute path to the pattern beatmap file.
    /// </summary>
    /// <param name="fileName">The name of the pattern beatmap file.</param>
    /// <returns>The absolute path to the pattern beatmap file.</returns>
    public string GetPatternPath(string fileName) {
        return Path.Combine(GetPatternFilesFolderPath(), fileName);
    }

    /// <summary>
    /// Gets the relative path from the base folder to the pattern beatmap file.
    /// </summary>
    /// <param name="fileName">The name of the pattern beatmap file.</param>
    /// <returns>The relative path from the base folder to the pattern beatmap file.</returns>
    public string GetPatternRelativePath(string fileName) {
        return Path.Combine(GetPatternFilesFolderRelativePath(), fileName);
    }

    /// <summary>
    /// Gets the names of all the collection folders in the pattern folder
    /// </summary>
    /// <returns>All collection folder names.</returns>
    public string[] GetCollectionFolderNames() {
        return Directory.GetDirectories(BasePath)
            .Select(Path.GetFileName).ToArray();
    }

    /// <summary>
    /// Checks if a collection folder with a certain name exists.
    /// </summary>
    /// <param name="name">The folder name to look for.</param>
    /// <returns>Whether the collect folder with specified name exists.</returns>
    public bool CollectionFolderExists(string name) {
        return GetCollectionFolderNames().Contains(name);
    }

    /// <summary>
    /// Renames <see cref="CollectionFolderName"/> and updates the file system.
    /// </summary>
    /// <param name="newName">The new collection folder name.</param>
    public void RenameCollectionFolder(string newName) {
        if (CollectionFolderName == newName) return;

        if (CollectionFolderExists(newName)) {
            throw new DuplicateNameException($"A collection with the name \"{newName}\" already exists in {BasePath}.");
        }

        Directory.Move(GetCollectionFolderPath(), Path.Combine(BasePath, newName));
        CollectionFolderName = newName;
    }

    /// <inheritdoc/>
    public IBeatmap GetPatternBeatmap(string filename) {
        return new BeatmapEditor(GetPatternPath(filename)).ReadFile();
    }

    /// <inheritdoc/>
    public void SavePatternBeatmap(IBeatmap beatmap, string filename) {
        var editor = new WriteEditor<IBeatmap>(
            new OsuBeatmapEncoder(new OsuStoryboardEncoder(),
                new HitObjectEncoder(true),
                new TimingPointEncoder(true)),
            GetPatternPath(filename));

        editor.WriteFile(beatmap);
    }
}