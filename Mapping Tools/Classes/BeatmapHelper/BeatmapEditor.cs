using System.Collections.Generic;

namespace Mapping_Tools.Classes.BeatmapHelper {
    public class BeatmapEditor : Editor {
        public Beatmap Beatmap { get => (Beatmap)TextFile; }

        public BeatmapEditor(List<string> lines) {
            TextFile = new Beatmap(lines);
        }

        public BeatmapEditor(string path) {
            Path = path;
            TextFile = new Beatmap(ReadFile(Path));
        }
    }
}
