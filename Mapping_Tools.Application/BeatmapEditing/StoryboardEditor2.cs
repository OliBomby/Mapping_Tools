using Mapping_Tools.ApplicationServices.Abstractions;

namespace Mapping_Tools.Classes.BeatmapHelper;

public class StoryboardEditor2 : Editor2 {
    public StoryBoard StoryBoard => (StoryBoard)TextFile;

    public StoryboardEditor2(List<string> lines, ITextFileStore fileStore) : base(fileStore) {
        TextFile = new StoryBoard(lines);
    }

    public StoryboardEditor2(string path, ITextFileStore fileStore) : base(fileStore) {
        Path = path;
        TextFile = new StoryBoard(ReadFile(path));
    }
}
