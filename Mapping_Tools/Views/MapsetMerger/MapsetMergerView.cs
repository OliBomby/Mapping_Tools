using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.BeatmapHelper.Events;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Viewmodels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace Mapping_Tools.Views.ComboColourStudio;

/// <summary>
/// Interactielogica voor MapsetMergerView.xaml
/// </summary>
public partial class MapsetMergerView : ISavable<MapsetMergerVm> {
    public string AutoSavePath => Path.Combine(MainWindow.AppDataPath, "mapsetmergerproject.json");

    public string DefaultSaveFolder => Path.Combine(MainWindow.AppDataPath, "Mapset Merger Projects");

    public static readonly string ToolName = "Mapset Merger";

    public static readonly string ToolDescription = $@"Combine multiple mapsets into one mapset and automatically resolve file conflicts.";

    private MapsetMergerVm ViewModel => (MapsetMergerVm)DataContext;

    private const int MaxMapsetMaps = 200;

    public MapsetMergerView() {
        InitializeComponent();
        DataContext = new MapsetMergerVm();
        Width = MainWindow.AppWindow.ContentViews.Width;
        Height = MainWindow.AppWindow.ContentViews.Height;
        ProjectManager.LoadProject(this, message: false);
    }

    protected override void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
        var bgw = sender as BackgroundWorker;
        e.Result = MergeMapsets((MapsetMergerVm) e.Argument, bgw, e);
    }

    private void Start_Click(object sender, RoutedEventArgs e) {
        // Remove logical focus to trigger LostFocus on any fields that didn't yet update the ViewModel
        FocusManager.SetFocusedElement(FocusManager.GetFocusScope(this), null);

        BackgroundWorker.RunWorkerAsync(ViewModel);
        CanRun = false;
    }


    private static string MergeMapsets(MapsetMergerVm arg, BackgroundWorker worker, DoWorkEventArgs _) {
        int mapsetsMerged = 0;
        int indexStart = 1;

        ResolveDuplicateNames(arg.Mapsets);

        var usedNames = new HashSet<string>();

        foreach (var mapset in arg.Mapsets) {
            var subf = mapset.Name;
            var prefix = mapset.Name + " - ";

            var beatmaps = LoadBeatmaps(mapset);
            var storyboards = LoadStoryboards(mapset);

            // All hitsound indices in the beatmaps. Old index to new index
            var indices = new Dictionary<int, int>();
            // All hitsound files with custom indices
            var usedHsFiles = new HashSet<string>();
            // All explicitly referenced audio files like filename hs, SB samples
            var usedOtherHsFiles = new HashSet<string>();
            // All explicitly referenced image files like storyboard files, background
            var usedImageFiles = new HashSet<string>();
            // All explicitly referenced video files
            var usedVideoFiles = new HashSet<string>();

            // We have to ignore files which are not possible to reference in a distinguishing way
            // such as beatmap skin files and the spinnerspin and spinnerbonus files.

            StoryBoard sharedSb = null;
            if (arg.MoveSbToBeatmap) {
                sharedSb = storyboards.FirstOrDefault()?.Item2;

                if (sharedSb != null) {
                    GetUsedFilesAndUpdateReferences(sharedSb, subf, usedOtherHsFiles, usedImageFiles, usedVideoFiles);
                }
            } else {
                foreach (var storyboardTuple in storyboards) {
                    var storyboard = storyboardTuple.Item2;

                    GetUsedFilesAndUpdateReferences(storyboard, subf, usedOtherHsFiles, usedImageFiles, usedVideoFiles);

                    // Save storyboard in new location with unique filename
                    Editor.SaveFile(Path.Combine(arg.ExportPath, prefix + Path.GetFileName(storyboardTuple.Item1)),
                        storyboard.GetLines());
                }
            }

            // Find all used files and change references
            foreach (var beatmapTuple in beatmaps) {
                var beatmap = beatmapTuple.Item2;

                GetUsedFilesAndUpdateReferences(beatmap, subf, ref indexStart, indices, usedHsFiles, usedOtherHsFiles, usedImageFiles, usedVideoFiles);

                if (sharedSb != null) {
                    beatmap.StoryBoard.StoryboardLayerBackground = sharedSb.StoryboardLayerBackground;
                    beatmap.StoryBoard.StoryboardLayerForeground = sharedSb.StoryboardLayerForeground;
                    beatmap.StoryBoard.StoryboardLayerFail = sharedSb.StoryboardLayerFail;
                    beatmap.StoryBoard.StoryboardLayerPass = sharedSb.StoryboardLayerPass;
                    beatmap.StoryBoard.StoryboardLayerOverlay = sharedSb.StoryboardLayerOverlay;
                    beatmap.StoryBoard.StoryboardSoundSamples = sharedSb.StoryboardSoundSamples;
                }

                // Save beatmap in new location with unique diffname
                var diffname = beatmap.Metadata["Version"].Value;
                if (usedNames.Contains(diffname)) {
                    diffname = prefix + diffname;
                }

                usedNames.Add(diffname);
                beatmap.Metadata["Version"].Value = diffname;

                Editor.SaveFile(Path.Combine(arg.ExportPath, beatmap.GetFileName()),
                    beatmap.GetLines());
            }

            // Save assets in new location
            foreach (var filename in usedHsFiles) {
                var filepath = FindAssetFile(filename, mapset.Path, audioExtensions);

                if (filepath == null) {
                    continue;
                }

                var ext = Path.GetExtension(filepath);
                var extLess = Path.GetFileNameWithoutExtension(filepath);

                var match = Regex.Match(extLess, "^(normal|soft|drum)-(hit(normal|whistle|finish|clap)|slidertick|sliderslide)");

                var remainder = extLess.Substring(match.Index + match.Length);
                int index = 1;
                if (!string.IsNullOrWhiteSpace(remainder) && !FileFormatHelper.TryParseInt(remainder, out index)) {
                    continue;
                }

                var newFilename = indices[index] == 1 ?
                    extLess.Substring(0, match.Length) + ext :
                    extLess.Substring(0, match.Length) + indices[index] + ext;
                var newFilepath = Path.Combine(arg.ExportPath, newFilename);

                Directory.CreateDirectory(Path.GetDirectoryName(newFilepath));
                File.Copy(filepath, newFilepath, true);
            }

            foreach (var filename in usedOtherHsFiles) {
                SaveAsset(filename, mapset.Path, subf, arg.ExportPath, audioExtensions2);
            }

            foreach (var filename in usedImageFiles) {
                SaveAsset(filename, mapset.Path, subf, arg.ExportPath, imageExtensions);
            }

            foreach (var filename in usedVideoFiles) {
                SaveAsset(filename, mapset.Path, subf, arg.ExportPath, videoExtensions, true);
            }

            UpdateProgressBar(worker, ++mapsetsMerged * 100 / arg.Mapsets.Count);
        }

        // Make an accurate message
        var message = $"Successfully merged {mapsetsMerged} {(mapsetsMerged == 1 ? "mapset" : "mapsets")}!";
        return message;
    }

    private static void SaveAsset(string filename, string path, string subf, string exportPath, string[] extensions, bool needExtension = false) {
        var filepath = FindAssetFile(filename, path, extensions, needExtension);

        if (filepath == null) {
            return;
        }

        var ext = Path.GetExtension(filepath);
        var extLess = Path.ChangeExtension(filename, null);
        var newFilepath = Path.Combine(exportPath, subf, extLess + ext);

        Directory.CreateDirectory(Path.GetDirectoryName(newFilepath));
        File.Copy(filepath, newFilepath, true);
    }

    private static readonly string[] audioExtensions = { ".wav", ".mp3", ".ogg" };
    private static readonly string[] audioExtensions2 = { ".wav", ".ogg", ".mp3" }; // I swear to god, for some reason it prioritizes .ogg if it uses filename
    private static readonly string[] imageExtensions = { ".png", ".jpg" };
    private static readonly string[] videoExtensions = { ".mp4", ".avi" };

    private static string FindAssetFile(string filename, string path, string[] extensions, bool needExtension = false) {
        string filepath = Path.Combine(path, filename);
        string originalExt = Path.GetExtension(filename);
        string extLess = Path.ChangeExtension(filepath, null);

        if (!string.IsNullOrEmpty(originalExt) || needExtension) {
            if (!string.IsNullOrEmpty(originalExt) && extensions.Contains(originalExt) && File.Exists(filepath)) {
                return filepath;
            } else {
                return null;
            }
        }

        foreach (var ext in extensions) {
            filepath = Path.Combine(path, extLess + ext);
            if (File.Exists(filepath)) {
                return filepath;
            }
        }

        return null;
    }

    private static void GetUsedFilesAndUpdateReferences(StoryBoard storyboard, string subf, HashSet<string> usedOtherHsFiles, HashSet<string> usedImageFiles, HashSet<string> usedVideoFiles) {
        GetUsedFilesAndUpdateReferences(storyboard.BackgroundAndVideoEvents.Concat(storyboard.StoryboardSoundSamples)
            .Concat(storyboard.StoryboardLayerFail).Concat(storyboard.StoryboardLayerPass).Concat(storyboard.StoryboardLayerBackground)
            .Concat(storyboard.StoryboardLayerForeground).Concat(storyboard.StoryboardLayerOverlay), subf, usedOtherHsFiles, usedImageFiles, usedVideoFiles);
    }

    private static void GetUsedFilesAndUpdateReferences(IEnumerable<Event> events, string subf, HashSet<string> usedOtherHsFiles, HashSet<string> usedImageFiles, HashSet<string> usedVideoFiles) {
        foreach (var ev in events) {
            switch (ev) {
                case StoryboardSoundSample sbss:
                    usedOtherHsFiles.Add(sbss.FilePath);
                    sbss.FilePath = Path.Combine(subf, sbss.FilePath);
                    break;
                case Animation animation:
                    for (int i = 0; i < animation.FrameCount; i++) {
                        usedImageFiles.Add(Path.GetFileNameWithoutExtension(animation.FilePath) + i);
                    }
                    animation.FilePath = Path.Combine(subf, animation.FilePath);
                    break;
                case Sprite sprite:
                    usedImageFiles.Add(sprite.FilePath);
                    sprite.FilePath = Path.Combine(subf, sprite.FilePath);
                    break;
                case Background background:
                    usedImageFiles.Add(background.Filename);
                    background.Filename = Path.Combine(subf, background.Filename);
                    break;
                case Video video:
                    usedVideoFiles.Add(video.Filename);
                    video.Filename = Path.Combine(subf, video.Filename);
                    break;
            }

            if (ev.ChildEvents.Count > 0) {
                GetUsedFilesAndUpdateReferences(ev.ChildEvents, subf, usedOtherHsFiles, usedImageFiles, usedVideoFiles);
            }
        }
    }

    private static void GetUsedFilesAndUpdateReferences(Beatmap beatmap, string subf, ref int indexStart, Dictionary<int, int> indices, HashSet<string> usedHsFiles, HashSet<string> usedOtherHsFiles, HashSet<string> usedImageFiles, HashSet<string> usedVideoFiles) {
        GameMode mode = (GameMode)beatmap.General["Mode"].IntValue;
        double sliderTickRate = beatmap.Difficulty["SliderTickRate"].DoubleValue;

        usedOtherHsFiles.Add(beatmap.General["AudioFilename"].Value.Trim());
        beatmap.General["AudioFilename"].Value = " " + Path.Combine(subf, beatmap.General["AudioFilename"].Value.Trim());

        foreach (HitObject ho in beatmap.HitObjects) {
            usedHsFiles.UnionWith(ho.GetPlayingBodyFilenames(sliderTickRate, false));
        }

        foreach (TimelineObject tlo in beatmap.GetTimeline().TimelineObjects) {
            foreach (var filename in tlo.GetPlayingFilenames(mode, false)) {
                if (!string.IsNullOrEmpty(filename) && filename == tlo.Filename) {
                    usedOtherHsFiles.Add(filename);
                    tlo.Filename = Path.Combine(subf, tlo.Filename);
                    tlo.HitsoundsToOrigin();
                } else {
                    usedHsFiles.Add(filename);
                }
            }
        }

        // Adjust the remaining custom indices
        foreach (var ho in beatmap.HitObjects) {
            if (ho.CustomIndex == 0) {
                continue;
            }

            if (!indices.ContainsKey(ho.CustomIndex)) {
                indices[ho.CustomIndex] = indexStart++;
            }

            ho.CustomIndex = indices[ho.CustomIndex];
        }

        foreach (var tp in beatmap.BeatmapTiming) {
            if (tp.SampleIndex == 0) {
                continue;
            }

            if (!indices.ContainsKey(tp.SampleIndex)) {
                indices[tp.SampleIndex] = indexStart++;
            }

            tp.SampleIndex = indices[tp.SampleIndex];
        }

        GetUsedFilesAndUpdateReferences(beatmap.StoryBoard, subf, usedOtherHsFiles, usedImageFiles, usedVideoFiles);
    }

    private static IEnumerable<Tuple<string, Beatmap>> LoadBeatmaps(MapsetMergerVm.MapsetItem mapset) {
        // Check map count not over the max
        // Use generator pattern

        var beatmaps = Directory.GetFiles(mapset.Path, "*.osu", SearchOption.AllDirectories).ToList();

        if (beatmaps.Count > MaxMapsetMaps) {
            throw new Exception("Beatmap limit exceeded in mapset: " + mapset.Name);
        }

        foreach (var path in beatmaps) {
            yield return new Tuple<string, Beatmap>(path, new BeatmapEditor(path).Beatmap);
        }
    }

    private static IEnumerable<Tuple<string, StoryBoard>> LoadStoryboards(MapsetMergerVm.MapsetItem mapset) {
        // Check storyboard count not over the max
        // Use generator pattern

        var storyboards = Directory.GetFiles(mapset.Path, "*.osb", SearchOption.AllDirectories).ToList();

        if (storyboards.Count > MaxMapsetMaps) {
            throw new Exception("Storyboard limit exceeded in mapset: " + mapset.Name);
        }

        foreach (var path in storyboards) {
            yield return new Tuple<string, StoryBoard>(path, new StoryboardEditor(path).StoryBoard);
        }
    }

    private static void ResolveDuplicateNames(ICollection<MapsetMergerVm.MapsetItem> mapsets) {
        var names = new HashSet<string>();

        foreach (var mapset in mapsets) {
            if (names.Contains(mapset.Name)) {
                int i = 0;
                string originalName = mapset.Name;
                while (names.Contains(mapset.Name)) {
                    mapset.Name = originalName + ++i;
                }
            }

            names.Add(mapset.Name);
        }
    }

    public MapsetMergerVm GetSaveData() {
        return ViewModel;
    }

    public void SetSaveData(MapsetMergerVm saveData) {
        DataContext = saveData;
    }
}