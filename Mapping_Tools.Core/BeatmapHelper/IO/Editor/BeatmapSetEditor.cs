using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Mapping_Tools.Core.BeatmapHelper.IO.Editor;

/// <summary>
/// An editor that facilitates reading and writing <see cref="BeatmapSetInfo"/> from a folder in the file system.
/// </summary>
public class BeatmapSetEditor : IReadWriteEditor<BeatmapSetInfo> {
    protected readonly PathEditor<IBeatmap> beatmapEditor;
    protected readonly PathEditor<IStoryboard> storyboardEditor;

    /// <summary>
    /// The path to the the containing folder of the beatmap set.
    /// </summary>
    public string RootPath { get; set; }

    public BeatmapSetEditor() : this(null) { }

    public BeatmapSetEditor(string rootPath) : this(
        new BeatmapEditor(),
        new StoryboardEditor(),
        rootPath) { }

    public BeatmapSetEditor(
        PathEditor<IBeatmap> beatmapEditor,
        PathEditor<IStoryboard> storyboardEditor,
        string rootPath) {
        this.beatmapEditor = beatmapEditor;
        this.storyboardEditor = storyboardEditor;
        RootPath = rootPath;
    }

    public BeatmapSetInfo ReadFile() {
        var allFiles = Directory.GetFiles(RootPath, "*.*", SearchOption.AllDirectories);
        var beatmapFiles = allFiles.Where(p => p.EndsWith(".osu", StringComparison.OrdinalIgnoreCase));
        var storyboardFiles = allFiles.Where(p => p.EndsWith(".osb", StringComparison.OrdinalIgnoreCase));

        var beatmaps = new Dictionary<string, IBeatmap>();
        foreach (var path in beatmapFiles) {
            var relativePath = Path.GetRelativePath(RootPath, path);
            beatmapEditor.Path = path;
            beatmaps.Add(relativePath, beatmapEditor.ReadFile());
        }

        var storyboards = new Dictionary<string, IStoryboard>();
        foreach (var path in storyboardFiles) {
            var relativePath = Path.GetRelativePath(RootPath, path);
            storyboardEditor.Path = path;
            storyboards.Add(relativePath, storyboardEditor.ReadFile());
        }

        return new BeatmapSetInfo {
            Files = allFiles
                .Select(p => Path.GetRelativePath(RootPath, p))
                .Select(p => (IBeatmapSetFileInfo) new BeatmapSetFileInfo(RootPath, p))
                .ToList(),
            Beatmaps = beatmaps,
            Storyboards = storyboards
        };
    }

    public void WriteFile(BeatmapSetInfo instance) {
        // Only write the beatmaps and storyboards
        // The other files are read-only

        if (instance.Beatmaps != null) {
            foreach (var (relativePath, beatmap) in instance.Beatmaps) {
                beatmapEditor.Path = Path.Combine(RootPath, relativePath);
                beatmapEditor.WriteFile(beatmap);
            }
        }

        if (instance.Storyboards != null) {
            foreach (var (relativePath, storyboard) in instance.Storyboards) {
                storyboardEditor.Path = Path.Combine(RootPath, relativePath);
                storyboardEditor.WriteFile(storyboard);
            }
        }
    }
}