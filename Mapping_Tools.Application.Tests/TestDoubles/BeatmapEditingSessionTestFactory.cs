using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Serialization;

namespace Mapping_Tools.Application.Tests.TestDoubles;

internal static class BeatmapEditingSessionTestFactory
{
    public static Beatmap DecodeText(string text)
    {
        return new BeatmapDecoder().Decode(text);
    }

    public static HitObject DecodeHitObject(string line, int formatVersion = 14)
    {
        string text = $"osu file format v{formatVersion}\r\n\r\n[TimingPoints]\r\n\r\n[HitObjects]\r\n{line}";
        return DecodeText(text).HitObjects.Single();
    }

    public static Beatmap CloneThroughText(Beatmap beatmap)
    {
        return new BeatmapDecoder().Decode(new BeatmapEncoder().Encode(beatmap));
    }

    public static BeatmapEditingSession FromModel(
        Beatmap beatmap,
        string path,
        ITextFileStore fileStore,
        BeatmapEditingSource source = BeatmapEditingSource.Disk,
        IReadOnlyList<HitObject>? selectedHitObjects = null,
        Exception? liveReadFailure = null,
        double? liveEditorTime = null)
    {
        return new BeatmapEditingSession(
            beatmap,
            path,
            fileStore,
            new BeatmapEncoder(),
            source,
            selectedHitObjects ?? [],
            liveReadFailure,
            liveEditorTime);
    }

    public static BeatmapEditingSession FromPath(
        string path,
        ITextFileStore fileStore,
        BeatmapEditingSource source = BeatmapEditingSource.Disk,
        IReadOnlyList<HitObject>? selectedHitObjects = null,
        Exception? liveReadFailure = null,
        double? liveEditorTime = null)
    {
        return new BeatmapEditingSession(
            path,
            fileStore,
            new BeatmapDecoder(),
            new BeatmapEncoder(),
            source,
            selectedHitObjects,
            liveReadFailure,
            liveEditorTime);
    }

    public static BeatmapEditingSession FromText(
        string text,
        string path,
        ITextFileStore fileStore,
        BeatmapEditingSource source = BeatmapEditingSource.Disk,
        IReadOnlyList<HitObject>? selectedHitObjects = null,
        Exception? liveReadFailure = null,
        double? liveEditorTime = null)
    {
        Beatmap beatmap = new BeatmapDecoder().Decode(text);
        return FromModel(
            beatmap,
            path,
            fileStore,
            source,
            selectedHitObjects,
            liveReadFailure,
            liveEditorTime);
    }

    public static StoryboardEditingSession StoryboardFromPath(
        string path,
        ITextFileStore fileStore,
        int targetVersion = 128)
    {
        return new StoryboardEditingSession(
            path,
            fileStore,
            new StoryboardDecoder(),
            new StoryboardEncoder(),
            targetVersion);
    }
}
