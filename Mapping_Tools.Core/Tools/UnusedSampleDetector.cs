using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.BeatmapHelper.Events;
using Mapping_Tools.Core.BeatmapHelper.HitObjects.Objects;
using Mapping_Tools.Core.BeatmapHelper.TimelineStuff;

namespace Mapping_Tools.Core.Tools;

public class UnusedSampleDetector {
    public static IEnumerable<IBeatmapSetFileInfo> FindUnusedSamples([NotNull] BeatmapSetInfo beatmapSet) {
        if (beatmapSet.SoundFiles == null) {
            yield break;
        }

        // Collect all the used samples
        HashSet<string> allFilenames = new HashSet<string>();

        if (beatmapSet.Beatmaps != null)
            foreach (var (path, beatmap) in beatmapSet.Beatmaps) {
                var containingFolder = Path.GetDirectoryName(path) ?? string.Empty;
                GameMode mode = beatmap.General.Mode;
                double sliderTickRate = beatmap.Difficulty.SliderTickRate;

                // Only if there are spinners in standard you may have spinnerspin and spinnerbonus
                if (mode == 0 && beatmap.HitObjects.Any(o => o is Spinner))
                    allFilenames.UnionWith(new[] { "spinnerspin", "spinnerbonus" });

                allFilenames.Add(Path.Combine(containingFolder, beatmap.General.AudioFilename));

                foreach (Slider slider in beatmap.HitObjects.Where(ho => ho is Slider).Cast<Slider>()) {
                    allFilenames.UnionWith(slider.GetPlayingBodyFilenames(sliderTickRate, false)
                        .Select(o => Path.Combine(containingFolder, o)));
                }

                foreach (TimelineObject tlo in beatmap.GetTimeline().TimelineObjects) {
                    allFilenames.UnionWith(tlo.GetPlayingFilenames(mode, false)
                        .Select(o => Path.Combine(containingFolder, o)));
                }

                foreach (StoryboardSoundSample sbss in beatmap.Storyboard.StoryboardSoundSamples) {
                    allFilenames.Add(Path.Combine(containingFolder, sbss.FilePath));
                }
            }

        if (beatmapSet.Storyboards != null)
            foreach (var (path, storyboard) in beatmapSet.Storyboards) {
                var containingFolder = Path.GetDirectoryName(path) ?? string.Empty;

                foreach (StoryboardSoundSample sbss in storyboard.StoryboardSoundSamples) {
                    allFilenames.Add(Path.Combine(containingFolder, sbss.FilePath));
                }
            }

        // We DO extensions in osu! (kinda)
        // If the file with the exact extension doesn't exist we look for a file with the same name but a different extension

        // Find all the used samples
        var allSamples = beatmapSet.SoundFiles.ToList();
        var usedSamples = new HashSet<IBeatmapSetFileInfo>();

        foreach (var filename in allFilenames) {
            var sample = BeatmapSetInfo.GetSoundFile(allSamples, filename);
            if (sample != null) {
                usedSamples.Add(sample);
            }
        }

        // Find the unused samples
        foreach (var sample in allSamples) {
            string extless = Path.GetFileNameWithoutExtension(sample.Filename);
            if (!usedSamples.Contains(sample) &&
                !BeatmapSkinnableSamples.Any(o => Regex.IsMatch(extless!, o))) {
                yield return sample;
            }
        }
    }

    public static readonly string[] BeatmapSkinnableSamples = {
        "count1s",
        "count2s",
        "count3s",
        "gos",
        "readys",
        "applause",
        "comboburst",
        "comboburst-[0-9]+",
        "combobreak",
        "failsound",
        "sectionpass",
        "sectionfail",
        "pause-loop"
    };
}