using FluentAssertions;
using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.TumourGenerator;
using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Tools.TumourGenerating;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.TumourGenerator;

[TestClass]
public sealed class TumourGeneratorServiceTests
{
    [TestMethod]
    public async Task ImportAsync_SelectedModeRequiresLiveEditorAndReturnsSelectedSliders()
    {
        // Arrange
        FakeEditingGateway gateway = new(CreateSession(BeatmapEditingSource.LiveEditor));
        TumourGeneratorService service = new(gateway);

        // Act
        TumourImportResult result = await service.ImportAsync(
            "map.osu",
            TumourImportMode.Selected,
            null);

        // Assert
        gateway.LastPreference.Should().Be(LiveBeatmapPreference.RequireLive);
        result.UsedLiveEditor.Should().BeTrue();
        result.Sliders.Should().ContainSingle(item => item.IsSlider);
    }

    [TestMethod]
    public async Task ImportAsync_WhenSelectionContainsNoSliders_ReturnsEmptyState()
    {
        // Arrange
        FakeEditingGateway gateway = new(CreateSession(BeatmapEditingSource.LiveEditor, selectedSlider: false));
        TumourGeneratorService service = new(gateway);

        // Act
        TumourImportResult result = await service.ImportAsync(
            "map.osu",
            TumourImportMode.Selected,
            null);

        // Assert
        result.Sliders.Should().BeEmpty();
    }

    [TestMethod]
    public async Task PreviewAsync_CopiesInputAndReportsGeneratedLayerLengths()
    {
        // Arrange
        TumourGeneratorService service = new(new FakeEditingGateway(CreateSession(BeatmapEditingSource.Disk)));
        HitObject input = new("0,0,0,2,0,L|256:0,1,256");
        string original = input.Line;
        TumourGeneratorProject project = new();
        project.TumourLayers[0].TumourCount = 1;

        // Act
        TumourPreviewResult result = await service.PreviewAsync(input, project);

        // Assert
        input.Line.Should().Be(original);
        result.HitObject.Should().NotBeSameAs(input);
        result.HitObject.Line.Should().NotBe(original);
        result.LayerLengths.Should().ContainSingle();
    }

    [TestMethod]
    public async Task RunAsync_WithLiveSessionSavesAndRequestsEditorReloadWithProgress()
    {
        // Arrange
        FakeEditingGateway gateway = new(CreateSession(BeatmapEditingSource.LiveEditor));
        TumourGeneratorService service = new(gateway);
        TumourGeneratorProject project = new();
        project.TumourLayers[0].TumourCount = 1;
        List<double> progress = [];

        // Act
        TumourRunResult result = await service.RunAsync(
            ["map.osu"],
            project,
            reloadEditor: true,
            new Progress<double>(progress.Add));

        // Assert
        result.Paths.Should().Equal("map.osu");
        result.SlidersTumourated.Should().Be(1);
        result.EditorReloaded.Should().BeTrue();
        gateway.SaveReloadRequests.Should().ContainSingle().Which.Should().BeTrue();
        progress.Should().Contain(100);
    }

    [TestMethod]
    public async Task RunAsync_WithDiskSessionSavesWithoutEditorReload()
    {
        // Arrange
        FakeEditingGateway gateway = new(CreateSession(BeatmapEditingSource.Disk));
        TumourGeneratorService service = new(gateway);

        // Act
        TumourRunResult result = await service.RunAsync(
            ["map.osu"],
            new TumourGeneratorProject(),
            reloadEditor: true);

        // Assert
        result.EditorReloaded.Should().BeFalse();
        gateway.SaveReloadRequests.Should().ContainSingle().Which.Should().BeFalse();
    }

    [TestMethod]
    public async Task RunAsync_WhenCancelledBeforeOpening_StopsWithoutSaving()
    {
        // Arrange
        FakeEditingGateway gateway = new(CreateSession(BeatmapEditingSource.Disk));
        TumourGeneratorService service = new(gateway);
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        // Act
        Func<Task> act = () => service.RunAsync(
            ["map.osu"],
            new TumourGeneratorProject(),
            reloadEditor: false,
            cancellationToken: cancellation.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        gateway.SaveReloadRequests.Should().BeEmpty();
    }

    [TestMethod]
    public async Task PreviewAsync_WhenCancelled_StopsBeforeGeneration()
    {
        // Arrange
        TumourGeneratorService service = new(new FakeEditingGateway(CreateSession(BeatmapEditingSource.Disk)));
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        // Act
        Func<Task> act = () => service.PreviewAsync(
            new HitObject("0,0,0,2,0,L|256:0,1,256"),
            new TumourGeneratorProject(),
            cancellation.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private static BeatmapEditingSession CreateSession(
        BeatmapEditingSource source,
        bool selectedSlider = true)
    {
        List<string> lines =
        [
            "osu file format v14",
            "",
            "[General]",
            "Mode:0",
            "StackLeniency:0.7",
            "",
            "[Metadata]",
            "Version:Test",
            "",
            "[Difficulty]",
            "CircleSize:4",
            "SliderMultiplier:1.4",
            "SliderTickRate:1",
            "",
            "[TimingPoints]",
            "0,500,4,2,1,100,1,0",
            "",
            "[HitObjects]",
            "64,64,0,2,0,L|164:64,1,100",
            "128,128,500,1,0,0:0:0:0:"
        ];
        BeatmapEditor2 editor = new(lines, new MemoryTextFileStore());
        HitObject slider = editor.Beatmap.HitObjects[0];
        slider.IsSelected = selectedSlider;
        IReadOnlyList<HitObject> selected = selectedSlider ? [slider] : [editor.Beatmap.HitObjects[1]];
        return new BeatmapEditingSession(editor, source, selected);
    }

    private sealed class FakeEditingGateway(BeatmapEditingSession session) : IBeatmapEditingGateway
    {
        public BeatmapEditingSession Session { get; } = session;

        public LiveBeatmapPreference? LastPreference { get; private set; }

        public List<bool> SaveReloadRequests { get; } = [];

        public Task<BeatmapEditingSession> OpenBeatmapAsync(
            string path,
            LiveBeatmapPreference livePreference = LiveBeatmapPreference.PreferLive,
            CancellationToken cancellationToken = default)
        {
            LastPreference = livePreference;
            return Task.FromResult(Session);
        }

        public Task<StoryboardEditor2> OpenStoryboardAsync(
            string path,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task SaveAsync(
            Editor2 editor,
            bool reloadEditor = false,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task SaveAsync(
            BeatmapEditingSession session,
            bool reloadEditor = false,
            CancellationToken cancellationToken = default)
        {
            SaveReloadRequests.Add(reloadEditor);
            return Task.CompletedTask;
        }
    }

    private sealed class MemoryTextFileStore : ITextFileStore
    {
        public IReadOnlyList<string> ReadAllLines(string path) => [];

        public void WriteAllLines(string path, IEnumerable<string> lines)
        {
        }

        public void Delete(string path)
        {
        }

        public string GetParentFolder(string path) => string.Empty;

        public string CombinePath(string parent, string child) => child;
    }
}
