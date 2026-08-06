using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.HitsoundStuff;
using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Core.Tools.MapCleaner;

namespace Mapping_Tools.Classes.Tools.MapCleanerStuff;

/// <summary>Legacy WPF adapter over the shared Core Map Cleaner transformation.</summary>
public partial class MapCleaner
{
    public static MapCleanerResult CleanMap(
        BeatmapEditor editor,
        MapCleanerArgs args,
        BackgroundWorker worker = null)
    {
        MapCleanerOptions options = new()
        {
            VolumeSliders = args.VolumeSliders,
            SampleSetSliders = args.SampleSetSliders,
            VolumeSpinners = args.VolumeSpinners,
            ResnapObjects = args.ResnapObjects,
            ResnapBookmarks = args.ResnapBookmarks,
            AnalyzeSamples = args.AnalyzeSamples,
            RemoveUnusedSamples = args.RemoveUnusedSamples,
            RemoveHitsounds = args.RemoveHitsounds,
            RemoveMuting = args.RemoveMuting,
            RemoveUnclickableHitsounds = args.RemoveUnclickableHitsounds,
            BeatDivisors = args.BeatDivisors
        };
        string directory = editor.GetParentFolder();
        Dictionary<string, string> samples = HitsoundImporter.AnalyzeSamples(
            directory,
            false,
            args.AnalyzeSamples);
        Mapping_Tools.Core.Tools.MapCleaner.MapCleanerResult result =
            MapCleanerEngine.Clean(
                editor.Beatmap,
                options,
                directory,
                samples,
                worker is null
                    ? null
                    : new Progress<double>(value =>
                    {
                        if (worker.WorkerReportsProgress)
                            worker.ReportProgress((int)value);
                    }));
        int samplesRemoved = args.RemoveUnusedSamples
            ? RemoveUnusedSamples(directory, editor)
            : 0;
        return new MapCleanerResult(result.ObjectsResnapped, samplesRemoved);
    }

    public static int RemoveUnusedSamples(string mapDirectory, BeatmapEditor currentEditor)
    {
        HashSet<string> allFilenames = [];
        bool anySpinners = false;
        foreach (string path in Directory.GetFiles(mapDirectory, "*.osu", SearchOption.TopDirectoryOnly))
        {
            BeatmapEditor editor = path == currentEditor.Path ? currentEditor : new BeatmapEditor(path);
            Beatmap beatmap = editor.Beatmap;
            GameMode mode = (GameMode)beatmap.General["Mode"].IntValue;
            double tickRate = beatmap.Difficulty["SliderTickRate"].DoubleValue;
            anySpinners |= mode == GameMode.Standard && beatmap.HitObjects.Any(item => item.IsSpinner);
            allFilenames.Add(beatmap.General["AudioFilename"].Value.Trim());
            foreach (HitObject item in beatmap.HitObjects)
                allFilenames.UnionWith(item.GetPlayingBodyFilenames(tickRate, false));
            foreach (TimelineObject item in beatmap.GetTimeline().TimelineObjects)
                allFilenames.UnionWith(item.GetPlayingFilenames(mode, false));
            allFilenames.UnionWith(beatmap.StoryboardSoundSamples.Select(sample => sample.FilePath));
        }
        foreach (string path in Directory.GetFiles(mapDirectory, "*.osb", SearchOption.TopDirectoryOnly))
        {
            StoryboardEditor editor = new(path);
            allFilenames.UnionWith(editor.StoryBoard.StoryboardSoundSamples.Select(sample => sample.FilePath));
        }
        if (anySpinners) allFilenames.UnionWith(["spinnerspin", "spinnerbonus"]);
        HashSet<string> used = new(
            allFilenames.Select(Path.GetFileNameWithoutExtension),
            StringComparer.OrdinalIgnoreCase);
        int removed = 0;
        foreach (FileInfo file in new DirectoryInfo(mapDirectory).GetFiles("*.*", SearchOption.TopDirectoryOnly)
                     .Where(file => new[] { ".wav", ".ogg", ".mp3" }.Contains(file.Extension, StringComparer.OrdinalIgnoreCase)))
        {
            string stem = Path.GetFileNameWithoutExtension(file.Name);
            if (used.Contains(stem) || BeatmapSkinnableSamples.Any(pattern => Regex.IsMatch(stem, pattern)))
                continue;
            file.Delete();
            removed++;
        }
        return removed;
    }

    public static readonly string[] BeatmapSkinnableSamples =
    [
        "count1s", "count2s", "count3s", "gos", "readys", "applause", "comboburst",
        "comboburst-[0-9]+", "combobreak", "failsound", "sectionpass", "sectionfail", "pause-loop"
    ];
}
