using System;
using System.IO;

namespace Mapping_Tools.Classes.Tools.PatternGallery {
    public class OsuPatternFileHandler {
        public string PatternFolderPath { get; set; }

        public string CollectionFolderName { get; set; }

        public OsuPatternFileHandler() { }

        public OsuPatternFileHandler(string collectionFolderName) {
            CollectionFolderName = collectionFolderName;
        }

        public OsuPatternFileHandler(string patternFolderPath, string collectionFolderName) {
            PatternFolderPath = patternFolderPath;
            CollectionFolderName = collectionFolderName;
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
    }
}