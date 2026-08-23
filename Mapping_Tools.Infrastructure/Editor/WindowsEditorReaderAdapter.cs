using System.ComponentModel;
using System.Diagnostics;
using Editor_Reader;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.GeometryDashboard;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Application.Workspace;
using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Core.Classes.MathUtil;
using Mapping_Tools.Infrastructure.Platform;
using DomainHitObject = Mapping_Tools.Core.Classes.BeatmapHelper.HitObject;
using ReaderHitObject = Editor_Reader.HitObject;

namespace Mapping_Tools.Infrastructure.Editor;

/// <summary>
///     Reads and validates the active osu!stable editor through Editor Reader,
///     then translates its vendor-specific memory model into application contracts.
/// </summary>
public sealed class WindowsEditorReaderAdapter :
    ILiveBeatmapReader,
    ICurrentBeatmapLocator,
    IGeometryDashboardEditorReader,
    IDisposable
{
    private readonly IApplicationDirectories _directories;
    private readonly Func<bool> _isWindows;
    private readonly object _lifecycleGate = new();
    private readonly EditorReader _reader = new();
    private readonly SemaphoreSlim _readerLock = new(1, 1);
    private readonly ApplicationSettings _settings;
    private int _activeReads;
    private bool _disposed;

    /// <summary>
    ///     Creates the Windows adapter with the Songs root used to reconstruct the
    ///     active beatmap path and an application-data location for diagnostics.
    /// </summary>
    /// <param name="settings">Settings containing the resolved osu! Songs path.</param>
    /// <param name="directories">Application-owned locations for validation logs.</param>
    public WindowsEditorReaderAdapter(
        ApplicationSettings settings,
        IApplicationDirectories directories)
        : this(settings, directories, OperatingSystem.IsWindows)
    {
    }

    internal WindowsEditorReaderAdapter(
        ApplicationSettings settings,
        IApplicationDirectories directories,
        Func<bool> isWindows)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _directories = directories ?? throw new ArgumentNullException(nameof(directories));
        _isWindows = isWindows ?? throw new ArgumentNullException(nameof(isWindows));
    }

    /// <inheritdoc />
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
    ///     Releases the synchronization primitive owned by this singleton adapter.
    /// </summary>
    public void Dispose()
    {
        bool disposeReaderLock;
        lock (_lifecycleGate)
        {
            if (_disposed) return;

            _disposed = true;
            disposeReaderLock = _activeReads == 0;
        }

        if (disposeReaderLock) _readerLock.Dispose();
    }

    /// <inheritdoc />
    public async Task<GeometryDashboardEditorSnapshot?> ReadGeometryDashboardAsync(
        GeometryDashboardProcess process,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(process);
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_isWindows()) return null;

        EnterRead();
        bool lockTaken = false;
        try
        {
            await _readerLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            lockTaken = true;
            using var nativeProcess = OsuProcessDiscovery.FindStableProcess(process.ProcessId);
            if (nativeProcess is null || !IsActiveEditor(nativeProcess, process.MainWindow))
                return null;

            cancellationToken.ThrowIfCancellationRequested();
            var snapshot = await Task.Run(
                    () => ReadGeometrySnapshot(nativeProcess),
                    cancellationToken)
                .ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return snapshot;
        }
        finally
        {
            if (lockTaken) _readerLock.Release();

            ExitRead();
        }
    }

    /// <inheritdoc />
    public async Task<LiveBeatmapSnapshot?> ReadAsync(
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_isWindows()) return null;

        EnterRead();
        bool lockTaken = false;
        try
        {
            await _readerLock.WaitAsync(cancellationToken).ConfigureAwait(false);
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
            if (lockTaken) _readerLock.Release();

            ExitRead();
        }
    }

    private void EnterRead()
    {
        lock (_lifecycleGate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _activeReads++;
        }
    }

    private void ExitRead()
    {
        bool disposeReaderLock;
        lock (_lifecycleGate)
        {
            _activeReads--;
            disposeReaderLock = _disposed && _activeReads == 0;
        }

        if (disposeReaderLock) _readerLock.Dispose();
    }

    private LiveBeatmapSnapshot ReadSnapshot(Process process)
    {
        _reader.SetProcess(process);
        _reader.autoDeStack = true;
        _reader.FetchAll();

        try
        {
            return EditorReaderSnapshotConverter.Convert(
                _reader,
                _settings.SongsPath,
                _reader.EditorTime());
        }
        catch (InvalidDataException)
        {
            WriteDiagnosticLog(_reader);
            throw;
        }
    }

    private GeometryDashboardEditorSnapshot ReadGeometrySnapshot(Process process)
    {
        var snapshot = ReadSnapshot(process);
        int editorTime = snapshot.EditorTime is null
            ? 0
            : checked((int)snapshot.EditorTime.Value);
        return new GeometryDashboardEditorSnapshot(
            snapshot.Path,
            _reader.ApproachRate,
            _reader.CircleSize,
            editorTime,
            snapshot.HitObjects,
            snapshot.SelectedHitObjects);
    }

    private static bool IsActiveEditor(
        Process process,
        PlatformWindowId? expectedWindow = null)
    {
        try
        {
            return (expectedWindow is null || process.MainWindowHandle.ToInt64() == expectedWindow.Value.Value)
                   && process.MainWindowTitle.EndsWith(".osu", StringComparison.Ordinal);
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
