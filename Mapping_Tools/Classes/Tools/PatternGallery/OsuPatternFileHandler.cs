using System;
using System.Data;
using System.IO;
using System.Linq;
using Mapping_Tools.Classes.MathUtil;
using Newtonsoft.Json;

namespace Mapping_Tools.Classes.Tools.PatternGallery {
    public class OsuPatternFileHandler {
        public string PatternFilesFolderName => @"Pattern Files";

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
    }
}