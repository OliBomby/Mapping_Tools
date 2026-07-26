using System.Diagnostics;
using Editor_Reader;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Application.Workspace;
using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Core.Classes.MathUtil;
using DomainHitObject = Mapping_Tools.Core.Classes.BeatmapHelper.HitObject;
using ReaderHitObject = Editor_Reader.HitObject;

namespace Mapping_Tools.Infrastructure.Editor;

/// <summary>
/// Reads and validates the active osu!stable editor through Editor Reader,
/// then translates its vendor-specific memory model into application contracts.
/// </summary>
public sealed class WindowsEditorReaderAdapter :
    ILiveBeatmapReader,
    ICurrentBeatmapLocator,
    IDisposable
{
    private readonly ApplicationSettings _settings;
    private readonly IApplicationDirectories _directories;
    private readonly EditorReader _reader = new();
    private readonly SemaphoreSlim _readerLock = new(1, 1);
    private bool _disposed;

    /// <summary>
    /// Creates the Windows adapter with the Songs root used to reconstruct the
    /// active beatmap path and an application-data location for diagnostics.
    /// </summary>
    /// <param name="settings">Settings containing the resolved osu! Songs path.</param>
    /// <param name="directories">Application-owned locations for validation logs.</param>
    public WindowsEditorReaderAdapter(
        ApplicationSettings settings,
        IApplicationDirectories directories)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _directories = directories ?? throw new ArgumentNullException(nameof(directories));
    }

    /// <inheritdoc/>
    public async Task<LiveBeatmapSnapshot?> ReadAsync(
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        await _readerLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using Process? process = OsuProcessDiscovery.FindStableProcess();
            if (process is null ||
                !process.MainWindowTitle.EndsWith(".osu", StringComparison.Ordinal))
            {
                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();
            LiveBeatmapSnapshot snapshot = await Task.Run(
                    () => ReadSnapshot(process),
                    cancellationToken)
                .ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return snapshot;
        }
        finally
        {
            _readerLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<string?> FindCurrentBeatmapAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            return (await ReadAsync(cancellationToken).ConfigureAwait(false))?.Path;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Releases the synchronization primitive owned by this singleton adapter.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _readerLock.Dispose();
        _disposed = true;
    }

    private LiveBeatmapSnapshot ReadSnapshot(Process process)
    {
        _reader.SetProcess(process);
        _reader.autoDeStack = true;
        _reader.FetchAll();

        try
        {
            return EditorReaderSnapshotConverter.Convert(_reader, _settings.SongsPath);
        }
        catch (InvalidDataException)
        {
            WriteDiagnosticLog(_reader);
            throw;
        }
    }

    private void WriteDiagnosticLog(EditorReader reader)
    {
        _directories.EnsureCreated();
        string path = Path.Combine(
            _directories.ApplicationData,
            "editor_reader_error.txt");
        List<string> lines =
        [
            $"ContainingFolder: {reader.ContainingFolder}",
            $"Filename: {reader.Filename}",
            $"ApproachRate: {reader.ApproachRate}",
            $"CircleSize: {reader.CircleSize}",
            $"HPDrainRate: {reader.HPDrainRate}",
            $"OverallDifficulty: {reader.OverallDifficulty}",
            $"PreviewTime: {reader.PreviewTime}",
            $"SliderMultiplier: {reader.SliderMultiplier}",
            $"SliderTickRate: {reader.SliderTickRate}",
            $"StackLeniency: {reader.StackLeniency}",
            $"TimelineZoom: {reader.TimelineZoom}",
            $"numBookmarks: {reader.numBookmarks}",
            $"numClipboard: {reader.numClipboard}",
            $"numControlPoints: {reader.numControlPoints}",
            $"numObjects: {reader.numObjects}",
            $"numSelected: {reader.numSelected}",
            $"EditorTime: {reader.EditorTime()}",
            $"ProcessTitle: {reader.ProcessTitle()}",
            "[HitObjects]"
        ];
        lines.AddRange(
            reader.hitObjects?.ToList().Select(item => item.ToString())
            ?? []);
        lines.Add("[TimingPoints]");
        lines.AddRange(
            reader.controlPoints?.ToList().Select(item => item.ToString())
            ?? []);
        File.WriteAllLines(path, lines);
    }
}

internal static class EditorReaderSnapshotConverter
{
    internal static LiveBeatmapSnapshot Convert(
        EditorReader reader,
        string songsPath)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentException.ThrowIfNullOrWhiteSpace(songsPath);

        int removed = reader.hitObjects.RemoveAll(IsInvalid);
        if (removed > 1 ||
            reader.numControlPoints <= 0 ||
            reader.controlPoints is null ||
            reader.hitObjects is null ||
            reader.numControlPoints != reader.controlPoints.Count ||
            reader.numObjects != reader.hitObjects.Count ||
            reader.hitObjects.Any(IsInvalid))
        {
            throw new InvalidDataException(
                "Editor Reader returned inconsistent object or timing-point data.");
        }

        string path = Path.Combine(
            songsPath,
            reader.ContainingFolder,
            reader.Filename);
        return new LiveBeatmapSnapshot(
            path,
            reader.bookmarks.Select(value => (double)value).ToList(),
            reader.controlPoints.Select(ConvertControlPoint).ToList(),
            reader.hitObjects.Select(ConvertHitObject).ToList(),
            reader.PreviewTime,
            reader.SliderMultiplier,
            reader.SliderTickRate);
    }

    private static bool IsInvalid(ReaderHitObject hitObject)
    {
        return hitObject.SegmentCount > 9000 ||
               hitObject.Type == 0 ||
               hitObject.SampleSet > 1000 ||
               hitObject.SampleSetAdditions > 1000 ||
               hitObject.SampleVolume > 1000;
    }

    private static TimingPoint ConvertControlPoint(ControlPoint controlPoint)
    {
        return new TimingPoint(
            controlPoint.Offset,
            controlPoint.BeatLength,
            controlPoint.TimeSignature,
            (SampleSet)controlPoint.SampleSet,
            controlPoint.CustomSamples,
            controlPoint.Volume,
            controlPoint.TimingChange,
            (controlPoint.EffectFlags & 1) > 0,
            (controlPoint.EffectFlags & 8) > 0);
    }

    private static DomainHitObject ConvertHitObject(ReaderHitObject source)
    {
        DomainHitObject hitObject = new()
        {
            PixelLength = source.SpatialLength,
            Time = source.StartTime,
            ObjectType = source.Type,
            EndTime = source.EndTime,
            Hitsounds = source.SoundType,
            Pos = new Vector2(source.X, source.Y),
            EndPos = new Vector2(source.X, source.Y),
            Filename = source.SampleFile,
            SampleVolume = source.SampleVolume,
            SampleSet = (SampleSet)source.SampleSet,
            AdditionSet = (SampleSet)source.SampleSetAdditions,
            CustomIndex = source.CustomSampleSet,
            IsSelected = source.IsSelected
        };

        if (hitObject.IsSlider)
        {
            hitObject.Repeat = source.SegmentCount;
            hitObject.SliderType = (PathType)source.CurveType;
            if (source.sliderCurvePoints is not null)
            {
                hitObject.CurvePoints =
                    new List<Vector2>(source.sliderCurvePoints.Length / 2);
                for (int index = 1;
                     index < source.sliderCurvePoints.Length / 2;
                     index++)
                {
                    hitObject.CurvePoints.Add(
                        new Vector2(
                            source.sliderCurvePoints[index * 2],
                            source.sliderCurvePoints[index * 2 + 1]));
                }
            }

            hitObject.EdgeHitsounds =
                source.SoundTypeList?.ToList() ?? [];
            hitObject.EdgeSampleSets = source.SampleSetList is null
                ? []
                : Array.ConvertAll(
                        source.SampleSetList,
                        value => (SampleSet)value)
                    .ToList();
            hitObject.EdgeAdditionSets =
                source.SampleSetAdditionsList is null
                    ? []
                    : Array.ConvertAll(
                            source.SampleSetAdditionsList,
                            value => (SampleSet)value)
                        .ToList();

            Pad(hitObject.EdgeHitsounds, hitObject.Repeat + 1, 0);
            Pad(hitObject.EdgeSampleSets, hitObject.Repeat + 1, SampleSet.None);
            Pad(hitObject.EdgeAdditionSets, hitObject.Repeat + 1, SampleSet.None);
        }
        else
        {
            hitObject.Repeat =
                hitObject.IsSpinner || hitObject.IsHoldNote ? 1 : 0;
        }

        return hitObject;
    }

    private static void Pad<T>(List<T> values, int count, T defaultValue)
    {
        while (values.Count < count)
        {
            values.Add(defaultValue);
        }
    }
}

internal static class OsuProcessDiscovery
{
    internal static Process? FindStableProcess()
    {
        foreach (Process process in Process.GetProcessesByName("osu!"))
        {
            try
            {
                ProcessModule? mainModule = process.MainModule;
                if (mainModule is not null &&
                    string.Equals(
                        mainModule.ModuleName,
                        "osu!.exe",
                        StringComparison.Ordinal) &&
                    string.Equals(
                        mainModule.FileVersionInfo.ProductName,
                        "osu!",
                        StringComparison.Ordinal))
                {
                    return process;
                }
            }
            catch (InvalidOperationException)
            {
            }
            catch (System.ComponentModel.Win32Exception)
            {
            }

            process.Dispose();
        }

        return null;
    }
}
