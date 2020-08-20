using System;
using System.Data;
using System.IO;
using System.Linq;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.Tools.PatternGallery {
    public class OsuPatternFileHandler {
        public string PatternFolderPath { get; set; }

        public string CollectionFolderName { get; set; }

        public OsuPatternFileHandler() {
            CollectionFolderName = RNG.RandomString(20);
        }

        public OsuPatternFileHandler(string patternFolderPath) {
            CollectionFolderName = RNG.RandomString(20);
            PatternFolderPath = patternFolderPath;
            EnsureCollectionFolderExists();
        }

        public void EnsureCollectionFolderExists() {
            Directory.CreateDirectory(GetCollectionFolderPath());
        }

        public string GetCollectionFolderPath() {
            return Path.Combine(PatternFolderPath, CollectionFolderName);
        }

        public string GetPatternPath(string fileName) {
            return Path.Combine(PatternFolderPath, CollectionFolderName, fileName);
        }

        /// <summary>
        /// Gets the names of all the collection folders in the pattern folder
        /// </summary>
        /// <returns></returns>
        public string[] GetCollectionFolderNames() {
            return Directory.GetDirectories(PatternFolderPath)
                .Select(Path.GetFileName).ToArray();
        }

        public bool CollectionFolderExists(string name) {
            return GetCollectionFolderNames().Contains(name);
        }

        public void RenameCollection(string newName) {
            if (CollectionFolderName == newName) return;

            if (CollectionFolderExists(newName)) {
                throw new DuplicateNameException($"A collection with the name {newName} already exists in {PatternFolderPath}.");
            }

            Directory.Move(GetCollectionFolderPath(), Path.Combine(PatternFolderPath, newName));
            CollectionFolderName = newName;
        }
    }
}