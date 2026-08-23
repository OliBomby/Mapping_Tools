using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.ComboColourStudio;
using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Tools.ComboColourStudio;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.ComboColourStudio;

[TestClass]
public sealed class ComboColourStudioServiceTests
{
    [TestMethod]
    public async Task ApplyAsync_WithMapAndValidProject_UsesLivePreferenceAndSavesProgress()
    {
        // Arrange
        BeatmapEditor editor = new(
            File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Beatmaps", "standard-feature-rich.osu")).ToList(),
            new MemoryStore()) { Path = @"C:\set\map.osu" };
        RecordingGateway gateway = new(editor);
        ComboColourProject project = new();
        project.ComboColours.Clear();
        project.ComboColours.Add(new SpecialColour(RgbaColour.FromRgb(10, 20, 30), "Combo1"));
        project.AddColourPoint(0, [project.ComboColours[0]]);
        List<double> progress = [];
        ComboColourStudioService service = new(gateway);

        // Act
        var result = await service.ApplyAsync(
            [editor.Path],
            project,
            new Progress<double>(progress.Add));

        // Assert
        result.ProcessedCount.Should().Be(1);
        gateway.Preference.Should().Be(LiveBeatmapPreference.PreferLive);
        gateway.Saved.Should().BeSameAs(editor);
        gateway.ReloadEditor.Should().BeFalse();
        editor.Beatmap.ComboColours.Single().Color.Should().Be(RgbaColour.FromRgb(10, 20, 30));
        progress.Should().Equal(100);
    }

    [TestMethod]
    public async Task ApplyAsync_WithoutTargetMaps_ThrowsValidationException()
    {
        // Arrange
        BeatmapEditor editor = new(
            File.ReadAllLines(Path.Combine(
                AppContext.BaseDirectory,
                "Fixtures",
                "Beatmaps",
                "standard-feature-rich.osu")).ToList(),
            new MemoryStore());
        RecordingGateway gateway = new(editor);
        ComboColourProject project = new();
        project.AddComboColour();
        ComboColourStudioService service = new(gateway);

        // Act
        Func<Task> act = () => service.ApplyAsync([], project);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    private sealed class RecordingGateway(BeatmapEditor editor) : IBeatmapEditingGateway
    {
        public LiveBeatmapPreference? Preference { get; private set; }
        public Editor? Saved { get; private set; }
        public bool ReloadEditor { get; private set; }

        public Task<BeatmapEditingSession> OpenBeatmapAsync(
            string path,
            LiveBeatmapPreference livePreference = LiveBeatmapPreference.PreferLive,
            CancellationToken cancellationToken = default)
        {
            Preference = livePreference;
            return Task.FromResult(new BeatmapEditingSession(editor, BeatmapEditingSource.LiveEditor, []));
        }

        public Task<StoryboardEditor> OpenStoryboardAsync(
            string path,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task SaveAsync(
            Editor value,
            bool reloadEditor = false,
            CancellationToken cancellationToken = default)
        {
            Saved = value;
            ReloadEditor = reloadEditor;
            return Task.CompletedTask;
        }

        public Task SaveAsync(
            BeatmapEditingSession session,
            bool reloadEditor = false,
            CancellationToken cancellationToken = default)
        {
            return SaveAsync(session.Editor, reloadEditor, cancellationToken);
        }
    }

    private sealed class MemoryStore : ITextFileStore
    {
        public IReadOnlyList<string> ReadAllLines(string path)
        {
            throw new NotSupportedException();
        }

        public void WriteAllLines(string path, IEnumerable<string> lines) { }
        public void Delete(string path) { }

        public string GetParentFolder(string path)
        {
            return @"C:\set";
        }

        public string CombinePath(string parent, string child)
        {
            return Path.Combine(parent, child);
        }
    }
}
