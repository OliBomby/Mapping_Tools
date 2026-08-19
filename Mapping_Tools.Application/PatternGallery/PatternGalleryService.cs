using System.Text.RegularExpressions;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.ObjectVisualiser;
using Mapping_Tools.Application.Workspace;
using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Core.Tools.PatternGallery;

namespace Mapping_Tools.Application.PatternGallery;

/// <summary>
/// Coordinates Pattern Gallery's Core maker/placer with collection files,
/// Editor Reader state, safe beatmap saves, and the reusable object visualizer.
/// </summary>
public sealed class PatternGalleryService : IPatternGalleryService
{
    private readonly IBeatmapEditingGateway _editing;
    private readonly IPatternGalleryFileService _files;

    /// <summary>Creates the Pattern Gallery application use case.</summary>
    /// <param name="editing">Loads live or disk beatmaps and saves with backups.</param>
    /// <param name="files">Resolves collection files and performs file operations.</param>
    public PatternGalleryService(
        IBeatmapEditingGateway editing,
        IPatternGalleryFileService files)
    {
        _editing = editing ?? throw new ArgumentNullException(nameof(editing));
        _files = files ?? throw new ArgumentNullException(nameof(files));
    }

    /// <inheritdoc/>
    public async Task<ObjectVisualiserScene> LoadSceneAsync(
        PatternGalleryPattern pattern,
        PatternGalleryCollectionPaths paths,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        BeatmapEditingSession session = await _editing.OpenBeatmapAsync(
                _files.GetPatternPath(paths, pattern.FileName),
                LiveBeatmapPreference.DiskOnly,
                cancellationToken)
            .ConfigureAwait(false);
        session.Editor.Beatmap.UpdateStacking();
        return ObjectVisualiserSceneBuilder.FromHitObjects(
            session.Editor.Beatmap.HitObjects.Take(100),
            session.Editor.Beatmap.Difficulty["CircleSize"].DoubleValue,
            useStackedPositions: true);
    }

    /// <inheritdoc/>
    public Task<PatternGalleryPattern> ImportCodeAsync(
        string name,
        string hitObjectText,
        string timingPointText,
        double globalSv,
        GameMode gameMode,
        PatternGalleryProject project,
        PatternGalleryCollectionPaths paths,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(project);
        cancellationToken.ThrowIfCancellationRequested();
        List<HitObject> hitObjects = ParseLines(hitObjectText, line => new HitObject(line));
        List<TimingPoint> timingPoints = ParseLines(timingPointText, line => new TimingPoint(line));
        PatternGalleryMaker maker = new() { Padding = project.Padding };
        PatternGalleryPattern pattern = maker.FromObjects(
            hitObjects,
            timingPoints,
            name,
            globalSv,
            gameMode,
            out Beatmap patternBeatmap);
        SavePattern(pattern, patternBeatmap, paths, cancellationToken);
        return Task.FromResult(pattern);
    }

    /// <inheritdoc/>
    public async Task<PatternGalleryPattern> ImportFileAsync(
        string sourcePath,
        string name,
        string? filter,
        double startTime,
        double endTime,
        PatternGalleryCollectionPaths paths,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        BeatmapEditingSession source = await _editing.OpenBeatmapAsync(
                sourcePath,
                LiveBeatmapPreference.DiskOnly,
                cancellationToken)
            .ConfigureAwait(false);
        PatternGalleryMaker maker = new();
        PatternGalleryPattern pattern;
        if (!string.IsNullOrEmpty(filter) || startTime != -1 || endTime != -1)
        {
            pattern = maker.FromBeatmapFiltered(
                source.Editor.Beatmap,
                name,
                filter,
                startTime,
                endTime,
                out Beatmap filtered);
            SavePattern(pattern, filtered, paths, cancellationToken);
        }
        else
        {
            pattern = maker.FromBeatmap(source.Editor.Beatmap, name);
            _files.CopyPattern(sourcePath, _files.GetPatternPath(paths, pattern.FileName));
        }

        return pattern;
    }

    /// <inheritdoc/>
    public async Task<PatternGalleryPattern> ImportSelectedAsync(
        string sourcePath,
        string name,
        PatternGalleryCollectionPaths paths,
        CancellationToken cancellationToken = default)
    {
        BeatmapEditingSession source = await _editing.OpenBeatmapAsync(
                sourcePath,
                LiveBeatmapPreference.RequireLive,
                cancellationToken)
            .ConfigureAwait(false);
        PatternGalleryMaker maker = new();
        PatternGalleryPattern pattern = maker.FromSelected(
            source.Editor.Beatmap,
            name,
            out Beatmap filtered);
        SavePattern(pattern, filtered, paths, cancellationToken);
        return pattern;
    }

    /// <inheritdoc/>
    public async Task<PatternGalleryRunResult> ExportAsync(
        string targetPath,
        IReadOnlyList<PatternGalleryPattern> patterns,
        PatternGalleryProject project,
        PatternGalleryCollectionPaths paths,
        bool quick,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetPath);
        ArgumentNullException.ThrowIfNull(patterns);
        ArgumentNullException.ThrowIfNull(project);
        if (patterns.Count == 0)
        {
            throw new InvalidOperationException("No pattern has been selected to export.");
        }

        LiveBeatmapPreference preference = project.ExportTimeMode == ExportTimeMode.Current
            ? LiveBeatmapPreference.RequireLive
            : LiveBeatmapPreference.PreferLive;
        BeatmapEditingSession target = await _editing.OpenBeatmapAsync(
                targetPath,
                preference,
                cancellationToken)
            .ConfigureAwait(false);
        double exportTime = project.ExportTimeMode switch
        {
            ExportTimeMode.Current => target.LiveEditorTime
                ?? throw new InvalidOperationException("Could not fetch the current editor time."),
            ExportTimeMode.Custom => project.CustomExportTime,
            ExportTimeMode.Pattern => 0,
            _ => throw new ArgumentOutOfRangeException(nameof(project.ExportTimeMode))
        };
        PatternGalleryPlacer placer = project.CreatePlacer();
        for (int index = 0; index < patterns.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            PatternGalleryPattern pattern = patterns[index];
            BeatmapEditingSession source = await _editing.OpenBeatmapAsync(
                    _files.GetPatternPath(paths, pattern.FileName),
                    LiveBeatmapPreference.DiskOnly,
                    cancellationToken)
                .ConfigureAwait(false);
            if (project.ExportTimeMode == ExportTimeMode.Pattern)
            {
                placer.PlaceOsuPattern(source.Editor.Beatmap, target.Editor.Beatmap, protectBeatmapPattern: false);
            }
            else
            {
                placer.PlaceOsuPatternAtTime(
                    source.Editor.Beatmap,
                    target.Editor.Beatmap,
                    exportTime,
                    protectBeatmapPattern: false);
            }

            pattern.UseCount++;
            pattern.LastUsedTime = DateTime.Now;
            progress?.Report((index + 1) * 100d / patterns.Count);
        }

        await _editing.SaveAsync(target, reloadEditor: quick, cancellationToken)
            .ConfigureAwait(false);
        return new PatternGalleryRunResult(patterns.Count, "Successfully exported pattern!");
    }

    /// <inheritdoc/>
    public Task DeleteAsync(
        IReadOnlyList<PatternGalleryPattern> patterns,
        PatternGalleryCollectionPaths paths,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(patterns);
        foreach (PatternGalleryPattern pattern in patterns)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _files.DeletePattern(_files.GetPatternPath(paths, pattern.FileName));
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<PatternGalleryRestoreResult> RestoreAsync(
        PatternGalleryProject project,
        PatternGalleryCollectionPaths paths,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        string[] actual = _files.EnumeratePatternFiles(paths).ToArray();
        HashSet<string> actualSet = actual.ToHashSet(StringComparer.OrdinalIgnoreCase);
        List<PatternGalleryPattern> removed = project.Patterns
            .Where(pattern => !actualSet.Contains(pattern.FileName))
            .ToList();
        foreach (PatternGalleryPattern pattern in removed)
        {
            project.Patterns.Remove(pattern);
        }

        HashSet<string> indexed = project.Patterns
            .Select(pattern => pattern.FileName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        PatternGalleryMaker maker = new();
        int added = 0;
        foreach (string filename in actual.Where(name => !indexed.Contains(name)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            BeatmapEditingSession session = await _editing.OpenBeatmapAsync(
                    _files.GetPatternPath(paths, filename),
                    LiveBeatmapPreference.DiskOnly,
                    cancellationToken)
                .ConfigureAwait(false);
            PatternGalleryPattern pattern = maker.FromBeatmap(
                session.Editor.Beatmap,
                Path.GetFileNameWithoutExtension(filename).Split("__").LastOrDefault() ?? filename,
                filename);
            project.Patterns.Add(pattern);
            added++;
        }

        return new PatternGalleryRestoreResult(removed.Count, added);
    }

    private void SavePattern(
        PatternGalleryPattern pattern,
        Beatmap patternBeatmap,
        PatternGalleryCollectionPaths paths,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _files.EnsureCollection(paths);
        patternBeatmap.SaveWithFloatPrecision = true;
        string destination = _files.GetPatternPath(paths, pattern.FileName);
        _files.WritePatternBytes(
            destination,
            System.Text.Encoding.UTF8.GetBytes(string.Join("\r\n", patternBeatmap.GetLines())));
    }

    private static List<T> ParseLines<T>(string? text, Func<string, T> parse)
    {
        List<T> result = [];
        foreach (string line in Regex.Split(text ?? string.Empty, "\\r?\\n"))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                result.Add(parse(line.Trim()));
            }
            catch
            {
                // The legacy dialog ignores malformed individual lines and
                // reports an error only when no usable hit object remains.
            }
        }

        return result;
    }
}
