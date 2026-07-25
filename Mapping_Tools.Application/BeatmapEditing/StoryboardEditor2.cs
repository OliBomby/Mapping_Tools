using Mapping_Tools.ApplicationServices.Abstractions;

namespace Mapping_Tools.Classes.BeatmapHelper;

/// <summary>
/// Provides typed loading and saving for an osu! storyboard document.
/// </summary>
public class StoryboardEditor2 : Editor2 {
    /// <summary>
    /// Gets the parsed storyboard document.
    /// </summary>
    public StoryBoard StoryBoard => (StoryBoard)TextFile;

    /// <summary>
    /// Creates a storyboard editor from serialized lines.
    /// </summary>
    /// <param name="lines">The storyboard lines to parse.</param>
    /// <param name="fileStore">The persistence implementation used when saving.</param>
    public StoryboardEditor2(List<string> lines, ITextFileStore fileStore) : base(fileStore) {
        TextFile = new StoryBoard(lines);
    }

    /// <summary>
    /// Loads a storyboard through the supplied persistence boundary.
    /// </summary>
    /// <param name="path">The storyboard file to load.</param>
    /// <param name="fileStore">The persistence implementation used to load and save.</param>
    public StoryboardEditor2(string path, ITextFileStore fileStore) : base(fileStore) {
        Path = path;
        TextFile = new StoryBoard(ReadFile(path));
    }
}
