using System.Security.Cryptography;
using Mapping_Tools.Application.MapCleaner;
using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Classes.BeatmapHelper.Enums;

namespace Mapping_Tools.Infrastructure.Files;

/// <summary>Analyzes mapset samples and recoverably moves unused audio out of the live folder.</summary>
public sealed class PhysicalMapCleanerSampleService : IMapCleanerSampleService
{
    private static readonly string[] AudioExtensions = [".wav", ".ogg", ".mp3"];

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
                    : new Beatmap(File.ReadAllLines(path).ToList());
                CollectUsed(beatmap, used, ref anyStandardSpinner);
            }

            foreach (string path in Directory.EnumerateFiles(directory, "*.osb"))
            {
                StoryBoard storyboard = new(File.ReadAllLines(path).ToList());
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
        foreach (var item in beatmap.HitObjects) used.UnionWith(item.GetPlayingBodyFilenames(tickRate, false).Select(Path.GetFileNameWithoutExtension));

        foreach (var item in beatmap.GetTimeline().TimelineObjects) used.UnionWith(item.GetPlayingFilenames(mode, false).Select(Path.GetFileNameWithoutExtension));
        used.UnionWith(beatmap.StoryboardSoundSamples.Select(sample =>
            Path.GetFileNameWithoutExtension(sample.FilePath)));
    }

    private static bool IsAudio(string path)
    {
        return AudioExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsSkinnable(string stem)
    {
        return stem is
                   "count1s" or "count2s" or "count3s" or "gos" or "readys" or "applause" or
                   "combobreak" or "failsound" or "sectionpass" or "sectionfail" or "pause-loop"
               || stem.StartsWith("comboburst", StringComparison.OrdinalIgnoreCase);
    }
}
