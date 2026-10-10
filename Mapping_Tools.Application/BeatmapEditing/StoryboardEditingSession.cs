using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Serialization;

namespace Mapping_Tools.Application.BeatmapEditing;

/// <summary>
///     Provides typed loading and saving for an osu! storyboard document.
/// </summary>
public sealed class StoryboardEditingSession : EditingSession
{
    private readonly IStoryboardEncoder encoder;
    private readonly int targetVersion;

    /// <summary>
    ///     Loads a storyboard through the supplied persistence boundary.
    /// </summary>
    /// <param name="path">The storyboard file to load.</param>
    /// <param name="fileStore">The persistence implementation used to load and save.</param>
    /// <param name="decoder">The decoder used to create the storyboard model.</param>
    /// <param name="encoder">The encoder used to serialize the storyboard model.</param>
    /// <param name="targetVersion">The format version used to choose storyboard numeric precision.</param>
    public StoryboardEditingSession(
        string path,
        ITextFileStore fileStore,
        IStoryboardDecoder decoder,
        IStoryboardEncoder encoder,
        int targetVersion = 128)
        : this(DecodeFile(path, fileStore, decoder), path, fileStore, encoder, targetVersion)
    {
    }

    /// <summary>
    ///     Creates a storyboard editing session around an already decoded storyboard.
    /// </summary>
    /// <param name="storyboard">The mutable storyboard owned by the session.</param>
    /// <param name="path">The source or destination path for the storyboard.</param>
    /// <param name="fileStore">The persistence implementation used when saving.</param>
    /// <param name="encoder">The encoder used to serialize the storyboard model.</param>
    /// <param name="targetVersion">The format version used to choose storyboard numeric precision.</param>
    public StoryboardEditingSession(
        StoryBoard storyboard,
        string path,
        ITextFileStore fileStore,
        IStoryboardEncoder encoder,
        int targetVersion = 128)
        : base(fileStore)
    {
        StoryBoard = storyboard ?? throw new ArgumentNullException(nameof(storyboard));
        Path = path;
        this.encoder = encoder ?? throw new ArgumentNullException(nameof(encoder));
        this.targetVersion = targetVersion;
    }

    /// <summary>
    ///     Gets the parsed storyboard document.
    /// </summary>
    public StoryBoard StoryBoard { get; }

    /// <inheritdoc />
    protected override string EncodeDocument()
    {
        return encoder.Encode(StoryBoard, targetVersion);
    }

    private static StoryBoard DecodeFile(
        string path,
        ITextFileStore fileStore,
        IStoryboardDecoder decoder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(fileStore);
        ArgumentNullException.ThrowIfNull(decoder);
        return decoder.Decode(fileStore.ReadAllText(path));
    }
}
