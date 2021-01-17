using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Mapping_Tools_Core.Audio.DuplicateDetection {
    /// <summary>
    /// Duplicate detector that doesnt actually detect duplicates.
    /// It just makes the mapping from extension-less path to full path.
    /// </summary>
    public class UselessDuplicateSampleDetector : IDuplicateSampleDetector {
        public Dictionary<string, string> AnalyzeSamples(string dir, out Exception exception, bool includeSubdirectories) {
            var extList = GetSupportedExtensions();
            exception = null;

            List<string> samplePaths = Directory.GetFiles(dir, "*.*", 
                    includeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                .Where(n => extList.Contains(Path.GetExtension(n), StringComparer.OrdinalIgnoreCase)).ToList();

            Dictionary<string, string> dict = new Dictionary<string, string>();

            foreach (var samplePath in samplePaths) {
                string fullPathExtLess =
                    Path.Combine(Path.GetDirectoryName(samplePath) ?? throw new InvalidOperationException(),
                        Path.GetFileNameWithoutExtension(samplePath));
                dict[fullPathExtLess] = samplePath;
            }

            return dict;
        }

        public string[] GetSupportedExtensions() {
            return new[] { ".wav", ".ogg", ".mp3" };
        }
    }
}