using System.Collections.Generic;
using System.Linq;

namespace Mapping_Tools_Core.BeatmapHelper {
    /// <summary>
    /// Helper class for File Formats
    /// </summary>
    public static class FileFormatHelper {
        public static void AddDictionaryToLines(Dictionary<string, TValue> dict, List<string> lines) {
            lines.AddRange(EnumerateDictionary(dict));
        }

        public static IEnumerable<string> EnumerateDictionary(Dictionary<string, TValue> dict) {
            return dict.Select(kvp => kvp.Key + ":" + kvp.Value.Value);
        }

        public static void FillDictionary(Dictionary<string, TValue> dict, IEnumerable<string> lines) {
            foreach (var split in lines.Select(SplitKeyValue)) {
                dict[split[0]] = new TValue(split[1]);
            }
        }

        public static string[] SplitKeyValue(string line) {
            return line.Split(new[] { ':' }, 2);
        }

        public static IEnumerable<string> GetCategoryLines(IEnumerable<string> lines, string category, string[] categoryIdentifiers=null) {
            if (categoryIdentifiers == null)
                categoryIdentifiers = new[] { "[" };

            bool atCategory = false;

            foreach (string line in lines) {
                if (atCategory && line != "") {
                    if (categoryIdentifiers.Any(o => line.StartsWith(o))) // Reached another category
                    {
                        yield break;
                    }
                    yield return line;
                }
                else {
                    if (line == category) {
                        atCategory = true;
                    }
                }
            }
        }
    }
}
