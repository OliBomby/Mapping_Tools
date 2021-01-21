using System.IO;
using Mapping_Tools_Core.BeatmapHelper.Parsing;

namespace Mapping_Tools_Core.BeatmapHelper.Editor {
    public class BeatmapEditor : Editor<Beatmap> {
        public BeatmapEditor() : base(new OsuBeatmapParser()) {}

        public BeatmapEditor(string path) : base(new OsuBeatmapParser(), path) {}

        /// <summary>
        /// Saves the beatmap using <see cref=".SaveFile()"/> but also updates the filename according to the metadata of the <see cref="Beatmap"/>
        /// </summary>
        /// <remarks>This method also updates the Path property</remarks>
        public void WriteFileWithNameUpdate(Beatmap beatmap) {
            // Remove the beatmap with the old filename
            File.Delete(Path);

            // Save beatmap with the new filename
            Path = System.IO.Path.Combine(GetParentFolder(), beatmap.GetFileName());
            base.WriteFile(beatmap);
        }
    }
}
