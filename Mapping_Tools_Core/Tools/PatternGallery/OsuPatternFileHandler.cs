using Mapping_Tools_Core.BeatmapHelper;
using Mapping_Tools_Core.BeatmapHelper.Editor;
using Mapping_Tools_Core.BeatmapHelper.Encoding;
using Mapping_Tools_Core.MathUtil;
using Newtonsoft.Json;
using System.Data;
using System.IO;
using System.Linq;

namespace Mapping_Tools_Core.Tools.PatternGallery {
    public class OsuPatternFileHandler : IOsuPatternFileHandler {
        public static string PatternFilesFolderName => @"Pattern Files";

        [JsonIgnore]
        public string BasePath { get; set; }

        public string CollectionFolderName { get; set; }

        public OsuPatternFileHandler() {
            CollectionFolderName = RNG.RandomString(20);
        }

        public OsuPatternFileHandler(string basePath) {
            CollectionFolderName = RNG.RandomString(20);
            BasePath = basePath;
            EnsureCollectionFolderExists();
        }

        public void EnsureCollectionFolderExists() {
            Directory.CreateDirectory(GetPatternFilesFolderPath());
        }

        public string GetCollectionFolderPath() {
            return Path.Combine(BasePath, CollectionFolderName);
        }

        public string GetPatternFilesFolderPath() {
            return Path.Combine(BasePath, GetPatternFilesFolderRelativePath());
        }

        public string GetPatternFilesFolderRelativePath() {
            return Path.Combine(CollectionFolderName, PatternFilesFolderName);
        }

        public string GetPatternPath(string fileName) {
            return Path.Combine(GetPatternFilesFolderPath(), fileName);
        }

        public string GetPatternRelativePath(string fileName) {
            return Path.Combine(GetPatternFilesFolderRelativePath(), fileName);
        }

        /// <summary>
        /// Gets the names of all the collection folders in the pattern folder
        /// </summary>
        /// <returns></returns>
        public string[] GetCollectionFolderNames() {
            return Directory.GetDirectories(BasePath)
                .Select(Path.GetFileName).ToArray();
        }

        public bool CollectionFolderExists(string name) {
            return GetCollectionFolderNames().Contains(name);
        }

        public void RenameCollectionFolder(string newName) {
            if (CollectionFolderName == newName) return;

            if (CollectionFolderExists(newName)) {
                throw new DuplicateNameException($"A collection with the name \"{newName}\" already exists in {BasePath}.");
            }

            Directory.Move(GetCollectionFolderPath(), Path.Combine(BasePath, newName));
            CollectionFolderName = newName;
        }

        public IBeatmap GetPatternBeatmap(string filename) {
            return new BeatmapEditor(GetPatternPath(filename)).ReadFile();
        }

        public void SavePatternBeatmap(IBeatmap beatmap, string filename) {
            var editor = new WriteEditor<IBeatmap>(
                new OsuBeatmapEncoder {SaveWithFloatPrecision = true},
                GetPatternPath(filename));

            editor.WriteFile(beatmap);
        }
    }
}