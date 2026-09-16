using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Core.BeatmapHelper;

namespace Mapping_Tools.Application.BeatmapEditing;

/// <summary>
///     Provides typed loading and saving for an osu! storyboard document.
/// </summary>
public sealed class StoryboardEditingSession : EditingSession
{
    /// <summary>
    ///     Creates a storyboard editing session from serialized lines.
    /// </summary>
    /// <param name="lines">The storyboard lines to parse.</param>
    /// <param name="fileStore">The persistence implementation used when saving.</param>
    public StoryboardEditingSession(List<string> lines, ITextFileStore fileStore)
        : base(fileStore)
    {
        TextFile = new StoryBoard(lines);
    }

    /// <summary>
    ///     Loads a storyboard through the supplied persistence boundary.
    /// </summary>
    /// <param name="path">The storyboard file to load.</param>
    /// <param name="fileStore">The persistence implementation used to load and save.</param>
    public StoryboardEditingSession(string path, ITextFileStore fileStore)
        : base(fileStore)
    {
        Path = path;
        TextFile = new StoryBoard(ReadFile(path));
    }

    /// <summary>
    ///     Gets the parsed storyboard document.
    /// </summary>
    public StoryBoard StoryBoard => (StoryBoard)TextFile;
}
