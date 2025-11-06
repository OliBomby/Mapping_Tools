using System.IO;
using Mapping_Tools.Core.BeatmapHelper.IO.Decoding;
using Mapping_Tools.Core.BeatmapHelper.IO.Encoding;

namespace Mapping_Tools.Core.BeatmapHelper.IO.Editor;

public class BeatmapEditor : PathEditor<IBeatmap> {
    public BeatmapEditor() : base(new OsuBeatmapEncoder(), new OsuBeatmapDecoder()) {}

    public BeatmapEditor(string path) : base(new OsuBeatmapEncoder(), new OsuBeatmapDecoder(), path) {}

    /// <summary>
    /// Saves the beatmap using <see cref="!:PathEditor.WriteFile"/> but also updates the filename according to the metadata of the <see cref="IBeatmap"/>
    /// </summary>
    /// <remarks>This method also updates the Path property</remarks>
    public void WriteFileWithNameUpdate(IBeatmap beatmap, bool deleteOriginal = true) {
        // Remove the beatmap with the old filename
        if (deleteOriginal)
            File.Delete(Path);

        // Save beatmap with the new filename
        Path = System.IO.Path.Combine(GetParentFolder(), beatmap.GetFileName());
        base.WriteFile(beatmap);
    }
}