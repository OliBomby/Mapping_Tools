using System.ComponentModel;
using System.Diagnostics;
using Editor_Reader;
using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Workspace.Contracts;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Infrastructure.Tools.GeometryDashboard;
using OsuMemoryDataProvider;
using OsuMemoryDataProvider.OsuMemoryModels;
using OsuMemoryDataProvider.OsuMemoryModels.Direct;
using DomainHitObject = Mapping_Tools.Core.BeatmapHelper.HitObject;
using ReaderHitObject = Editor_Reader.HitObject;

namespace Mapping_Tools.Infrastructure.Editor;

/// <summary>
///     Reads the current osu!stable beatmap through its in-game memory model and
///     reads unsaved editor state through Editor Reader, then translates the
///     vendor-specific memory models into application contracts.
/// </summary>
public sealed class WindowsEditorReaderAdapter :
    ILiveBeatmapReader,
    ICurrentBeatmapLocator,
    IDisposable
{
    private readonly IApplicationDirectories directories;
    private readonly Func<Process?> findProcess;
    private readonly Func<bool> isWindows;
    private readonly object lifecycleGate = new();
    private readonly Func<Process, string?> readCurrentBeatmapFromMemory;
    private readonly EditorReader reader = new();
    private readonly SemaphoreSlim readerLock = new(1, 1);
    private readonly ApplicationSettings settings;
    private int activeReads;
    private bool disposed;

    /// <summary>
    ///     Creates the Windows adapter with the Songs root used to reconstruct the
    ///     active beatmap path and an application-data location for diagnostics.
    /// </summary>
    /// <param name="settings">Settings containing the resolved osu! Songs path.</param>
    /// <param name="directories">Application-owned locations for validation logs.</param>
    public WindowsEditorReaderAdapter(
        ApplicationSettings settings,
        IApplicationDirectories directories)
        : this(
            settings,
            directories,
            OperatingSystem.IsWindows,
            OsuProcessDiscovery.FindStableProcess,
            process => CurrentBeatmapMemoryReader.TryRead(process, settings.SongsPath))
    {
    }

    internal WindowsEditorReaderAdapter(
        ApplicationSettings settings,
        IApplicationDirectories directories,
        Func<bool> isWindows)
        : this(
            settings,
            directories,
            isWindows,
            OsuProcessDiscovery.FindStableProcess,
            process => CurrentBeatmapMemoryReader.TryRead(process, settings.SongsPath))
    {
    }

    internal WindowsEditorReaderAdapter(
        ApplicationSettings settings,
        IApplicationDirectories directories,
        Func<bool> isWindows,
        Func<Process?> findProcess,
        Func<Process, string?> readCurrentBeatmapFromMemory)
    {
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.directories = directories ?? throw new ArgumentNullException(nameof(directories));
        this.isWindows = isWindows ?? throw new ArgumentNullException(nameof(isWindows));
        this.findProcess = findProcess ?? throw new ArgumentNullException(nameof(findProcess));
        this.readCurrentBeatmapFromMemory = readCurrentBeatmapFromMemory
                                            ?? throw new ArgumentNullException(nameof(readCurrentBeatmapFromMemory));
    }

    /// <inheritdoc />
    public async Task<string?> FindCurrentBeatmapAsync(
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (!isWindows()) return null;

        EnterRead();
        bool lockTaken = false;
        try
        {
            await readerLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            lockTaken = true;
            using var process = findProcess();
            if (process is null) return null;

            cancellationToken.ThrowIfCancellationRequested();
            string? path = await Task.Run(
                    () => FindCurrentBeatmap(process),
                    cancellationToken)
                .ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return path;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return null;
        }
        finally
        {
            if (lockTaken) readerLock.Release();

            ExitRead();
        }
    }

    /// <summary>
    ///     Releases the synchronization primitive owned by this singleton adapter.
    /// </summary>
    public void Dispose()
    {
        bool disposeReaderLock;
        lock (lifecycleGate)
        {
            if (disposed) return;

            disposed = true;
            disposeReaderLock = activeReads == 0;
        }

        if (disposeReaderLock) readerLock.Dispose();
    }

    /// <inheritdoc />
    public async Task<LiveBeatmapSnapshot?> ReadAsync(
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (!isWindows()) return null;

        EnterRead();
        bool lockTaken = false;
        try
        {
            await readerLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            lockTaken = true;
            using var process = OsuProcessDiscovery.FindStableProcess();
            if (process is null || !IsActiveEditor(process)) return null;

            cancellationToken.ThrowIfCancellationRequested();
            var snapshot = await Task.Run(
                    () => ReadSnapshot(process),
                    cancellationToken)
                .ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return snapshot;
        }
        finally
        {
            if (lockTaken) readerLock.Release();

            ExitRead();
        }
    }

    private void EnterRead()
    {
        lock (lifecycleGate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            activeReads++;
        }
    }

    private void ExitRead()
    {
        bool disposeReaderLock;
        lock (lifecycleGate)
        {
            activeReads--;
            disposeReaderLock = disposed && activeReads == 0;
        }

        if (disposeReaderLock) readerLock.Dispose();
    }

    private LiveBeatmapSnapshot ReadSnapshot(Process process)
    {
        reader.SetProcess(process);
        reader.autoDeStack = true;
        reader.FetchAll();

        try
        {
            return EditorReaderSnapshotConverter.Convert(
                reader,
                settings.SongsPath,
                reader.EditorTime());
        }
        catch (InvalidDataException)
        {
            WriteDiagnosticLog(reader);
            throw;
        }
    }

    private string? FindCurrentBeatmap(Process process)
    {
        string? path = null;
        try
        {
            path = readCurrentBeatmapFromMemory(process);
        }
        catch
        {
            // Editor Reader is still a useful fallback when the in-game reader
            // cannot read the current beatmap object.
        }

        if (!string.IsNullOrWhiteSpace(path)) return path;

        if (string.IsNullOrWhiteSpace(settings.SongsPath)
            || !settings.UseEditorReader
            || !IsActiveEditor(process))
            return null;

        reader.SetProcess(process);
        reader.FetchHOM();
        reader.FetchBeatmap();
        return Path.Combine(
            settings.SongsPath,
            reader.ContainingFolder,
            reader.Filename);
    }

    private static bool IsActiveEditor(Process process)
    {
        try
        {
            return process.MainWindowTitle.EndsWith(".osu", StringComparison.Ordinal);
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (Win32Exception)
        {
            return false;
        }
    }

    private void WriteDiagnosticLog(EditorReader reader)
    {
        directories.EnsureCreated();
        string path = Path.Combine(
            directories.ApplicationData,
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
            "[HitObjects]",
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

internal static class CurrentBeatmapMemoryReader
{
    private static readonly StructuredOsuMemoryReader structuredReader =
        StructuredOsuMemoryReader.Instance;
    private static readonly OsuBaseAddresses osuBaseAddresses = new();
    private static readonly object readerGate = new();

    internal static string? TryRead(Process process, string songsPath)
    {
        ArgumentNullException.ThrowIfNull(process);
        if (string.IsNullOrWhiteSpace(songsPath)) return null;

        lock (readerGate)
        {
            string? folder = ReadString(
                osuBaseAddresses.Beatmap,
                nameof(CurrentBeatmap.FolderName));
            string? filename = ReadString(
                osuBaseAddresses.Beatmap,
                nameof(CurrentBeatmap.OsuFileName));
            if (string.IsNullOrWhiteSpace(folder)
                || string.IsNullOrWhiteSpace(filename))
                return null;

            return Path.Combine(songsPath, folder, filename);
        }
    }

    private static string? ReadString(object readObject, string propertyName)
    {
        return structuredReader.TryReadProperty(
            readObject,
            propertyName,
            out var readResult)
            ? readResult as string
            : null;
    }
}

internal static class EditorReaderSnapshotConverter
{
    internal static LiveBeatmapSnapshot Convert(
        EditorReader reader,
        string songsPath,
        double? editorTime = null)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentException.ThrowIfNullOrWhiteSpace(songsPath);

        if (reader.bookmarks is null
            || reader.controlPoints is null
            || reader.hitObjects is null
            || string.IsNullOrWhiteSpace(reader.ContainingFolder)
            || string.IsNullOrWhiteSpace(reader.Filename))
            throw new InvalidDataException(
                "Editor Reader returned incomplete editor metadata or collections.");

        int removed = reader.hitObjects.RemoveAll(IsInvalid);
        if (removed > 1
            || reader.numControlPoints <= 0
            || reader.numControlPoints != reader.controlPoints.Count
            || reader.numObjects != reader.hitObjects.Count
            || reader.hitObjects.Any(IsInvalid))
            throw new InvalidDataException(
                "Editor Reader returned inconsistent object or timing-point data.");

        string path = Path.Combine(
            songsPath,
            reader.ContainingFolder,
            reader.Filename);
        var hitObjects = reader.hitObjects.Select(ConvertHitObject).ToList();
        var selectedHitObjects = reader.hitObjects
            .Select((source, index) => source.IsSelected ? hitObjects[index] : null)
            .OfType<DomainHitObject>()
            .ToList();
        return new LiveBeatmapSnapshot(
            path,
            reader.bookmarks.Select(value => (double)value).ToList(),
            reader.controlPoints.Select(ConvertControlPoint).ToList(),
            hitObjects,
            reader.PreviewTime,
            reader.SliderMultiplier,
            reader.SliderTickRate,
            reader.ApproachRate,
            reader.CircleSize,
            editorTime,
            selectedHitObjects);
    }

    private static bool IsInvalid(ReaderHitObject hitObject)
    {
        return hitObject.SegmentCount > 9000 || hitObject.Type == 0 || hitObject.SampleSet > 1000 || hitObject.SampleSetAdditions > 1000 || hitObject.SampleVolume > 1000;
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
                    hitObject.CurvePoints.Add(
                        new Vector2(
                            source.sliderCurvePoints[index * 2],
                            source.sliderCurvePoints[index * 2 + 1]));
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
        while (values.Count < count) values.Add(defaultValue);
    }
}
