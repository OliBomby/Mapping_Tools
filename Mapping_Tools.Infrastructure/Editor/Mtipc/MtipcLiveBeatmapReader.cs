using System.Net.Sockets;
using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;

namespace Mapping_Tools.Infrastructure.Editor.Mtipc;

/// <summary>Reads unsaved beatmap state from an osu! MTIPC server.</summary>
public sealed class MtipcLiveBeatmapReader : ILiveBeatmapReader
{
    private readonly MtipcClient client = new();
    private readonly Func<string> songsPathProvider;

    /// <summary>Initializes an MTIPC reader using osu!'s songs directory.</summary>
    public MtipcLiveBeatmapReader(string songsPath)
    {
        this.songsPathProvider = () => songsPath;
    }

    /// <summary>Initializes an MTIPC reader using the live application settings.</summary>
    /// <param name="settings">Settings containing the osu! Songs directory.</param>
    public MtipcLiveBeatmapReader(ApplicationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        songsPathProvider = () => settings.SongsPath;
    }

    /// <inheritdoc />
    public async Task<LiveBeatmapSnapshot?> ReadAsync(CancellationToken cancellationToken = default) => await Task.Run(() =>
    {
        try
        {
            string songsPath = songsPathProvider();
            MtipcBeatmapData beatmap = client.ReadBeatmap(cancellationToken);
            var objects = client.ReadObjects(cancellationToken);
            var timingPoints = client.ReadControlPoints(cancellationToken);
            var bookmarks = client.ReadBookmarks(cancellationToken);
            double editorTime = client.ReadEditorTime(cancellationToken);
            if (string.IsNullOrWhiteSpace(songsPath)
                || string.IsNullOrWhiteSpace(beatmap.ContainingFolder)
                || string.IsNullOrWhiteSpace(beatmap.Filename))
                return null;

            var hitObjects = objects.Select(ConvertHitObject).ToList();
            return new LiveBeatmapSnapshot(Path.GetFullPath(Path.Combine(songsPath, beatmap.ContainingFolder, beatmap.Filename)), bookmarks,
                timingPoints.Select(ConvertTimingPoint).ToList(), hitObjects, beatmap.PreviewTime, beatmap.SliderMultiplier,
                beatmap.SliderTickRate, beatmap.ApproachRate, beatmap.CircleSize, editorTime,
                objects.Select((value, index) => value.IsSelected ? hitObjects[index] : null).OfType<HitObject>().ToList());
        }
        catch (OperationCanceledException) { throw; }
        catch (EndOfStreamException) { return null; }
        catch (SocketException) { return null; }
        catch (IOException) { return null; }
    }, cancellationToken);

    private static TimingPoint ConvertTimingPoint(MtipcControlPointData source) => new(source.Offset, source.BeatLength,
        source.TimeSignature, (SampleSet)source.SampleSet, source.CustomSamples, source.Volume,
        source.TimingChange, (source.EffectFlags & 1) != 0, (source.EffectFlags & 8) != 0);

    private static HitObject ConvertHitObject(MtipcHitObjectData source)
    {
        var hitObject = new HitObject
        {
            PixelLength = source.SpatialLength, Time = source.StartTime, ObjectType = source.Type,
            EndTime = source.EndTime, Hitsounds = source.SoundType, Pos = source.Position, EndPos = source.EndPosition,
            Filename = source.SampleFile ?? string.Empty, SampleVolume = source.SampleVolume,
            SampleSet = (SampleSet)source.SampleSet, AdditionSet = (SampleSet)source.SampleSetAdditions,
            CustomIndex = source.CustomSampleSet,
        };

        if (hitObject.IsSlider)
        {
            hitObject.Repeat = source.SegmentCount;
            hitObject.SliderType = (PathType)source.CurveType;
            hitObject.CurvePoints = source.CurvePoints.Skip(1).ToList();
            hitObject.EdgeHitsounds = source.SoundTypeList.ToList();
            hitObject.EdgeSampleSets = source.SampleSetList.Select(value => (SampleSet)value).ToList();
            hitObject.EdgeAdditionSets = source.SampleSetAdditionsList.Select(value => (SampleSet)value).ToList();
            Pad(hitObject.EdgeHitsounds, hitObject.Repeat + 1, 0);
            Pad(hitObject.EdgeSampleSets, hitObject.Repeat + 1, SampleSet.None);
            Pad(hitObject.EdgeAdditionSets, hitObject.Repeat + 1, SampleSet.None);
        }
        else hitObject.Repeat = hitObject.IsSpinner || hitObject.IsHoldNote ? 1 : 0;
        return hitObject;
    }

    private static void Pad<T>(List<T> values, int count, T defaultValue)
    {
        while (values.Count < count) values.Add(defaultValue);
    }
}
