using System.Security.Cryptography;
using Mapping_Tools.Application.Tools.MapCleaner;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Serialization;
using Mapping_Tools.Core.BeatmapHelper.Enums;

namespace Mapping_Tools.Infrastructure.Tools.MapCleaner;

/// <summary>Analyzes mapset samples and recoverably moves unused audio out of the live folder.</summary>
public sealed class PhysicalMapCleanerSampleService : IMapCleanerSampleService
{
    private static readonly string[] audioExtensions = [".wav", ".ogg", ".mp3"];
    private readonly IBeatmapDecoder beatmapDecoder;
    private readonly IStoryboardDecoder storyboardDecoder;

    /// <summary>Creates a sample analyzer that decodes beatmap and storyboard references.</summary>
    /// <param name="beatmapDecoder">Decodes beatmap files.</param>
    /// <param name="storyboardDecoder">Decodes storyboard files.</param>
    public PhysicalMapCleanerSampleService(
        IBeatmapDecoder beatmapDecoder,
        IStoryboardDecoder storyboardDecoder)
    {
        this.beatmapDecoder = beatmapDecoder ?? throw new ArgumentNullException(nameof(beatmapDecoder));
        this.storyboardDecoder = storyboardDecoder ?? throw new ArgumentNullException(nameof(storyboardDecoder));
    }

    /// <inheritdoc />
    public Task<IReadOnlyDictionary<string, string>> AnalyzeAsync(
        string directory,
        bool detectDuplicates,
        CancellationToken cancellationToken = default)
    {
        return Task.Run<IReadOnlyDictionary<string, string>>(() =>
        {
            string[] paths = Directory.EnumerateFiles(directory)
                .Where(IsAudio).ToArray();
            Dictionary<string, string> result = new(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, string> firstByHash = new(StringComparer.Ordinal);
            foreach (string path in paths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string first = path;
                if (detectDuplicates)
                {
                    using var stream = File.OpenRead(path);
                    string hash = Convert.ToHexString(SHA256.HashData(stream));
                    if (!firstByHash.TryGetValue(hash, out first!)) firstByHash[hash] = first = path;
                }

                result[Path.Combine(directory, Path.GetFileNameWithoutExtension(path))] = first;
            }

            return result;
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> MoveUnusedToRecoveryAsync(
        string directory,
        string currentBeatmapPath,
        Beatmap currentBeatmap,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            HashSet<string> used = new(StringComparer.OrdinalIgnoreCase);
            bool anyStandardSpinner = false;
            foreach (string path in Directory.EnumerateFiles(directory, "*.osu"))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var beatmap = string.Equals(
                    Path.GetFullPath(path),
                    Path.GetFullPath(currentBeatmapPath),
                    StringComparison.OrdinalIgnoreCase)
                    ? currentBeatmap
                    : beatmapDecoder.Decode(File.ReadAllText(path));
                CollectUsed(beatmap, used, ref anyStandardSpinner);
            }

            foreach (string path in Directory.EnumerateFiles(directory, "*.osb"))
            {
                StoryBoard storyboard = storyboardDecoder.Decode(File.ReadAllText(path));
                used.UnionWith(storyboard.StoryboardSoundSamples.Select(sample =>
                    Path.GetFileNameWithoutExtension(sample.FilePath)));
            }

            if (anyStandardSpinner) used.UnionWith(["spinnerspin", "spinnerbonus"]);

            string recovery = Path.Combine(
                directory,
                ".mapping-tools-unused-samples",
                DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff"));
            int moved = 0;
            foreach (string path in Directory.EnumerateFiles(directory).Where(IsAudio))
            {
                cancellationToken.ThrowIfCancellationRequested();
                string stem = Path.GetFileNameWithoutExtension(path);
                if (used.Contains(stem) || IsSkinnable(stem)) continue;

                Directory.CreateDirectory(recovery);
                File.Move(path, Path.Combine(recovery, Path.GetFileName(path)));
                moved++;
            }

            return moved;
        }, cancellationToken);
    }

    private static void CollectUsed(Beatmap beatmap, HashSet<string> used, ref bool anyStandardSpinner)
    {
        var mode = (GameMode)beatmap.General["Mode"].IntValue;
        double tickRate = beatmap.Difficulty["SliderTickRate"].DoubleValue;
        anyStandardSpinner |= mode == GameMode.Standard && beatmap.HitObjects.Any(item => item.IsSpinner);
        used.Add(Path.GetFileNameWithoutExtension(beatmap.General["AudioFilename"].Value.Trim()));
        foreach (var item in beatmap.HitObjects) used.UnionWith(item.GetPlayingBodyFilenames(tickRate, false).Select(Path.GetFileNameWithoutExtension).OfType<string>());

        foreach (var item in beatmap.GetTimeline().TimelineObjects) used.UnionWith(item.GetPlayingFilenames(mode, false).Select(Path.GetFileNameWithoutExtension).OfType<string>());
        used.UnionWith(beatmap.StoryboardSoundSamples.Select(sample =>
            Path.GetFileNameWithoutExtension(sample.FilePath)));
    }

    private static bool IsAudio(string path)
    {
        return audioExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsSkinnable(string stem)
    {
        return stem is
                   "count1s" or "count2s" or "count3s" or "gos" or "readys" or "applause" or
                   "combobreak" or "failsound" or "sectionpass" or "sectionfail" or "pause-loop"
               || stem.StartsWith("comboburst", StringComparison.OrdinalIgnoreCase);
    }
}
