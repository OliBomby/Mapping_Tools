using System.Text.RegularExpressions;
using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Tools.MapsetMerger;

namespace Mapping_Tools.Application.MapsetMerger;

/// <summary>Orchestrates Mapset Merger parsing, conflict resolution, and safe export.</summary>
public sealed class MapsetMergerService : IMapsetMergerService
{
    private const int MaxMapsetMaps = 200;
    private static readonly string[] AudioExtensions = [".wav", ".mp3", ".ogg"];
    private static readonly string[] ExplicitAudioExtensions = [".wav", ".ogg", ".mp3"];
    private static readonly string[] ImageExtensions = [".png", ".jpg"];
    private static readonly string[] VideoExtensions = [".mp4", ".avi"];

    private readonly IBeatmapEditingGateway _editingGateway;
    private readonly IMapsetFileSystem _fileSystem;
    private readonly ITextFileStore _textFileStore;

    /// <summary>Creates the export service.</summary>
    /// <param name="editingGateway">Loads disk-only beatmaps and storyboards.</param>
    /// <param name="fileSystem">Enumerates sources and owns staged output mutation.</param>
    /// <param name="textFileStore">Reads and writes the staged editor documents.</param>
    public MapsetMergerService(
        IBeatmapEditingGateway editingGateway,
        IMapsetFileSystem fileSystem,
        ITextFileStore textFileStore)
    {
        _editingGateway = editingGateway ?? throw new ArgumentNullException(nameof(editingGateway));
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _textFileStore = textFileStore ?? throw new ArgumentNullException(nameof(textFileStore));
    }

    /// <inheritdoc />
    public async Task<MapsetMergerResult> MergeAsync(
        MapsetMergerProject project,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        Validate(project);
        var inputs = project.Mapsets
            .Select(item => new MapsetMergerInput(item.Name, item.Path))
            .ToList();
        MapsetMergerEngine.ResolveDuplicateMapsetNames(inputs);
        ValidateExportPathDoesNotOverlapSources(project.ExportPath, inputs);

        using var transaction = _fileSystem.BeginTransaction(project.ExportPath);
        HashSet<string> usedDifficultyNames = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> usedOutputPaths = new(StringComparer.OrdinalIgnoreCase);
        int nextSampleIndex = 1;
        int beatmapsWritten = 0;
        int storyboardsWritten = 0;
        int assetsCopied = 0;
        progress?.Report(0);

        for (int mapsetIndex = 0; mapsetIndex < inputs.Count; mapsetIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var input = inputs[mapsetIndex];
            if (!_fileSystem.DirectoryExists(input.Path))
                throw new DirectoryNotFoundException(
                    $"Mapset directory '{input.Path}' was not found.");

            var beatmapPaths = _fileSystem.EnumerateFiles(input.Path, "*.osu");
            var storyboardPaths = _fileSystem.EnumerateFiles(input.Path, "*.osb");
            ValidateSourceFileCounts(input, beatmapPaths, storyboardPaths);

            List<(string Path, Beatmap Beatmap)> beatmaps = [];
            foreach (string path in beatmapPaths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var session = await _editingGateway
                    .OpenBeatmapAsync(path, LiveBeatmapPreference.DiskOnly, cancellationToken)
                    .ConfigureAwait(false);
                beatmaps.Add((path, session.Editor.Beatmap));
            }

            List<(string Path, StoryBoard Storyboard)> storyboards = [];
            foreach (string path in storyboardPaths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var editor = await _editingGateway
                    .OpenStoryboardAsync(path, cancellationToken)
                    .ConfigureAwait(false);
                storyboards.Add((path, editor.StoryBoard));
            }

            MapsetMergerReferences references = new();
            StoryBoard? sharedStoryboard = null;
            if (project.MoveSbToBeatmap)
            {
                sharedStoryboard = storyboards.Count == 0 ? null : storyboards[0].Storyboard;
                if (sharedStoryboard is not null)
                    references.Add(MapsetMergerEngine.RewriteStoryboardReferences(
                        sharedStoryboard,
                        input.Name));
            }
            else
            {
                foreach ((string path, var storyboard) in storyboards)
                {
                    references.Add(MapsetMergerEngine.RewriteStoryboardReferences(
                        storyboard,
                        input.Name));
                    string relativePath = ResolveOutputPath(
                        input.Name + " - " + Path.GetFileName(path),
                        usedOutputPaths);
                    // Save storyboard in new location with unique filename
                    WriteStoryboard(transaction, relativePath, storyboard);
                    storyboardsWritten++;
                }
            }

            Dictionary<int, int> sampleIndices = new();
            string prefix = input.Name + " - ";
            foreach ((string _, var beatmap) in beatmaps)
            {
                cancellationToken.ThrowIfCancellationRequested();
                references.Add(MapsetMergerEngine.RewriteBeatmapReferences(
                    beatmap,
                    input.Name,
                    ref nextSampleIndex,
                    sampleIndices));
                references.Add(MapsetMergerEngine.RewriteStoryboardReferences(
                    beatmap.StoryBoard,
                    input.Name));

                if (sharedStoryboard is not null)
                {
                    beatmap.StoryBoard.StoryboardLayerBackground = sharedStoryboard.StoryboardLayerBackground;
                    beatmap.StoryBoard.StoryboardLayerForeground = sharedStoryboard.StoryboardLayerForeground;
                    beatmap.StoryBoard.StoryboardLayerFail = sharedStoryboard.StoryboardLayerFail;
                    beatmap.StoryBoard.StoryboardLayerPass = sharedStoryboard.StoryboardLayerPass;
                    beatmap.StoryBoard.StoryboardLayerOverlay = sharedStoryboard.StoryboardLayerOverlay;
                    beatmap.StoryBoard.StoryboardSoundSamples = sharedStoryboard.StoryboardSoundSamples;
                }

                string version = beatmap.Metadata["Version"].Value;
                beatmap.Metadata["Version"].Value = MapsetMergerEngine
                    .ResolveDuplicateDifficultyName(version, prefix, usedDifficultyNames);
                string relativePath = ResolveOutputPath(beatmap.GetFileName(), usedOutputPaths);
                WriteBeatmap(transaction, relativePath, beatmap);
                beatmapsWritten++;
            }

            assetsCopied += CopyReferences(
                transaction,
                input,
                references,
                sampleIndices,
                cancellationToken);
            progress?.Report((mapsetIndex + 1) * 100d / inputs.Count);
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return new MapsetMergerResult(
            inputs.Count,
            beatmapsWritten,
            storyboardsWritten,
            assetsCopied);
    }

    private int CopyReferences(
        IMapsetFileTransaction transaction,
        MapsetMergerInput input,
        MapsetMergerReferences references,
        IReadOnlyDictionary<int, int> sampleIndices,
        CancellationToken cancellationToken)
    {
        int copied = 0;
        // Find all used files and change references
        foreach (string filename in OrderReferences(references.HitSoundFiles))
        {
            cancellationToken.ThrowIfCancellationRequested();
            string? source = FindAssetFile(filename, input.Path, AudioExtensions);
            if (source is null || !TryGetSourceSampleIndex(filename, out int sourceIndex) || !sampleIndices.TryGetValue(sourceIndex, out int mappedIndex))
                continue;

            string outputName = MapsetMergerEngine.GetRemappedHitsoundFilename(
                Path.GetFileName(source),
                mappedIndex);
            transaction.CopyToStaging(
                source,
                SafeRelativePath(outputName),
                cancellationToken);
            copied++;
        }

        foreach (string filename in OrderReferences(references.OtherAudioFiles))
            copied += CopyAsset(
                transaction,
                filename,
                input,
                input.Name,
                ExplicitAudioExtensions,
                cancellationToken: cancellationToken);

        foreach (string filename in OrderReferences(references.ImageFiles))
            copied += CopyAsset(
                transaction,
                filename,
                input,
                input.Name,
                ImageExtensions,
                cancellationToken: cancellationToken);

        foreach (string filename in OrderReferences(references.VideoFiles))
            copied += CopyAsset(
                transaction,
                filename,
                input,
                input.Name,
                VideoExtensions,
                true,
                cancellationToken);

        return copied;
    }

    private int CopyAsset(
        IMapsetFileTransaction transaction,
        string filename,
        MapsetMergerInput input,
        string outputFolder,
        IReadOnlyList<string> extensions,
        bool requireExtension = false,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string? source = FindAssetFile(filename, input.Path, extensions, requireExtension);
        if (source is null) return 0;

        // Save assets in new location
        string extension = Path.GetExtension(source);
        string extensionless = Path.ChangeExtension(filename, null);
        string relativePath = SafeRelativePath(Path.Combine(outputFolder, extensionless + extension));
        transaction.CopyToStaging(source, relativePath, cancellationToken);
        return 1;
    }

    private static IEnumerable<string> OrderReferences(IEnumerable<string> references)
    {
        return references
            .OrderBy(reference => reference, StringComparer.OrdinalIgnoreCase)
            .ThenBy(reference => reference, StringComparer.Ordinal);
    }

    private string? FindAssetFile(
        string filename,
        string sourceDirectory,
        IReadOnlyList<string> extensions,
        bool requireExtension = false)
    {
        string direct = Path.Combine(sourceDirectory, filename);
        string originalExtension = Path.GetExtension(filename);
        string extensionless = Path.ChangeExtension(direct, null);
        if (!string.IsNullOrEmpty(originalExtension) || requireExtension)
            return !string.IsNullOrEmpty(originalExtension) && extensions.Contains(originalExtension, StringComparer.OrdinalIgnoreCase) && _fileSystem.FileExists(direct)
                ? direct
                : null;

        // We have to ignore files which are not possible to reference in a distinguishable way
        // such as beatmap skin files and the spinnerspin and spinnerbonus files.
        return extensions
            .Select(extension => extensionless + extension)
            .FirstOrDefault(_fileSystem.FileExists);
    }

    private static bool TryGetSourceSampleIndex(string filename, out int index)
    {
        string extensionless = Path.GetFileNameWithoutExtension(filename);
        var match = Regex.Match(
            extensionless,
            "^(normal|soft|drum)-(hit(normal|whistle|finish|clap)|slidertick|sliderslide|sliderwhistle)(.*)$",
            RegexOptions.IgnoreCase);
        if (!match.Success || string.IsNullOrWhiteSpace(match.Groups[4].Value))
        {
            index = 1;
            return match.Success;
        }

        return int.TryParse(match.Groups[4].Value, out index) && index > 0;
    }

    private void WriteBeatmap(
        IMapsetFileTransaction transaction,
        string relativePath,
        Beatmap beatmap)
    {
        // Save beatmap in new location with unique diffname
        Editor2.SaveFile(
            _textFileStore,
            transaction.GetStagedPath(relativePath),
            beatmap.GetLines());
    }

    private void WriteStoryboard(
        IMapsetFileTransaction transaction,
        string relativePath,
        StoryBoard storyboard)
    {
        StoryboardEditor2 editor = new(storyboard.GetLines(), _textFileStore);
        editor.SaveFile(transaction.GetStagedPath(relativePath));
    }

    private static string ResolveOutputPath(string requestedPath, ISet<string> usedOutputPaths)
    {
        string safePath = SafeRelativePath(requestedPath);
        if (usedOutputPaths.Add(safePath)) return safePath;

        string directory = Path.GetDirectoryName(safePath) ?? string.Empty;
        string filename = Path.GetFileNameWithoutExtension(safePath);
        string extension = Path.GetExtension(safePath);
        int suffix = 1;
        string candidate;
        do
        {
            candidate = Path.Combine(directory, filename + suffix + extension);
        } while (!usedOutputPaths.Add(candidate));

        return candidate;
    }

    private static void Validate(MapsetMergerProject project)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentException.ThrowIfNullOrWhiteSpace(project.ExportPath);
        if (project.Mapsets is null || project.Mapsets.Count == 0) throw new ArgumentException("Add at least one mapset.", nameof(project));

        foreach (var item in project.Mapsets)
        {
            ArgumentNullException.ThrowIfNull(item);
            MapsetMergerEngine.ValidateMapsetName(item.Name);
            ArgumentException.ThrowIfNullOrWhiteSpace(item.Path);
        }
    }

    private static void ValidateExportPathDoesNotOverlapSources(
        string exportPath,
        IEnumerable<MapsetMergerInput> inputs)
    {
        string exportFullPath = Path.GetFullPath(exportPath);
        foreach (var input in inputs)
        {
            string sourceFullPath = Path.GetFullPath(input.Path);
            string relative = Path.GetRelativePath(sourceFullPath, exportFullPath);
            if (relative is "."
                || relative is not null
                && !relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    .Any(part => part is ".."))
                throw new InvalidOperationException(
                    $"The export directory '{exportPath}' must not be the same as or inside mapset '{input.Path}'.");
        }
    }

    private void ValidateSourceFileCounts(
        MapsetMergerInput input,
        IReadOnlyList<string> beatmaps,
        IReadOnlyList<string> storyboards)
    {
        if (!_fileSystem.DirectoryExists(input.Path)) throw new DirectoryNotFoundException($"Mapset directory '{input.Path}' was not found.");

        // Check map count not over the max
        if (beatmaps.Count > MaxMapsetMaps) throw new InvalidDataException("Beatmap limit exceeded in mapset: " + input.Name);

        // Check storyboard count not over the max
        if (storyboards.Count > MaxMapsetMaps) throw new InvalidDataException("Storyboard limit exceeded in mapset: " + input.Name);

        if (beatmaps.Count == 0) throw new InvalidDataException("No beatmaps were found in mapset: " + input.Name);
    }

    private static string SafeRelativePath(string path)
    {
        if (Path.IsPathRooted(path) || path.Split('/', '\\').Any(part => part is "..")) throw new InvalidDataException($"The output path '{path}' is not safe.");

        string root = Path.Combine(Path.GetTempPath(), "mapset-merger-relative-root");
        string normalized = Path.GetFullPath(Path.Combine(root, path));
        string relative = Path.GetRelativePath(root, normalized);
        if (relative is "." || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new InvalidDataException($"The output path '{path}' is not safe.");

        return relative;
    }
}
