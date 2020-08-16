using System;
using System.IO;

namespace Mapping_Tools.Classes.Tools.PatternGallery {
    public class OsuPatternFileHandler {
        public string PatternFolderPath { get; }

        public OsuPatternFileHandler(string patternFolderPath) {
            PatternFolderPath = patternFolderPath;
            EnsurePatternFolderExists();
        }

        private void EnsurePatternFolderExists() {
            Directory.CreateDirectory(PatternFolderPath);
        }

        public string GetPatternPath(string fileName) {
            return Path.Combine(PatternFolderPath, fileName);
        }
    }
}