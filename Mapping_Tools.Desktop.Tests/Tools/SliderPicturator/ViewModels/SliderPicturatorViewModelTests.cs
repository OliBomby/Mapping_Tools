using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Execution.UserNotification.Models;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tools.SliderPicturator;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.Images;
using Mapping_Tools.Desktop.Models;
using Mapping_Tools.Desktop.Shell;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.Tools.SliderPicturator.Models;
using Mapping_Tools.Desktop.Tools.SliderPicturator.ViewModels;
using Mapping_Tools.Desktop.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Tools.SliderPicturator.ViewModels;

[TestClass]
public sealed class SliderPicturatorViewModelTests
{
    [TestMethod]
    public async Task RunQuickAsync_WhenServiceReturnsSegmentCount_UpdatesSegmentCountFromResult()
    {
        // Arrange
        RecordingPicturator service = new() { ResultSegmentCount = 42 };
        var viewModel = Create(service, new RecordingCurrentBeatmapLocator("current.osu"));
        viewModel.SegmentCount = 3;

        // Act
        await viewModel.RunQuickAsync(CancellationToken.None);

        // Assert
        viewModel.SegmentCount.Should().Be(42);
    }

    [TestMethod]
    public void Install_WithPersistedSelectedSlider_RestoresSelectedSlider()
    {
        // Arrange
        var viewModel = Create(new RecordingPicturator(), new RecordingCurrentBeatmapLocator("current.osu"));
        HitObject selectedSlider = new("32,64,100,2,0,L|200:64,1,168");
        SliderPicturatorProject project = new() { SelectedSlider = selectedSlider };
        IShellProjectFeature<SliderPicturatorProject> feature = viewModel;

        // Act
        feature.Install(project);

        // Assert
        viewModel.SelectedSlider.Should().NotBeNull();
        viewModel.SelectedSlider!.Line.Should().Be(selectedSlider.Line);
    }

    [TestMethod]
    public void Activate_WithoutMapComboColors_DoesNotQueryLiveBeatmap()
    {
        // Arrange
        RecordingCurrentBeatmapLocator currentBeatmap = new();
        var viewModel = Create(new RecordingPicturator(), currentBeatmap);

        // Act
        viewModel.Activate();

        // Assert
        currentBeatmap.FindCount.Should().Be(0);
    }

    [TestMethod]
    public void Activate_WithMapComboColors_UsesSelectedWorkspaceMapWithoutLiveLookup()
    {
        // Arrange
        RecordingPicturator service = new()
        {
            AvailableColors = [RgbaColour.FromRgb(255, 0, 0)]
        };
        RecordingCurrentBeatmapLocator currentBeatmap = new();
        TestBeatmapWorkspace workspace = new();
        workspace.SetSelection(["selected.osu"]);
        var viewModel = Create(service, currentBeatmap, workspace);
        viewModel.UseMapComboColors = true;

        // Act
        viewModel.Activate();

        // Assert
        currentBeatmap.FindCount.Should().Be(0);
        service.ColorPaths.Should().ContainSingle().Which.Should().Be("selected.osu");
        viewModel.AvailableColors.Should().Equal(RgbaColour.FromRgb(255, 0, 0));
    }

    [TestMethod]
    public void WorkspaceSelectionChanged_WhenSelectionCleared_ClearsPaletteWithoutError()
    {
        // Arrange
        RgbaColour colour = RgbaColour.FromRgb(255, 0, 0);
        RecordingPicturator service = new() { AvailableColors = [colour] };
        RecordingCurrentBeatmapLocator currentBeatmap = new();
        TestBeatmapWorkspace workspace = new();
        workspace.SetSelection(["selected.osu"]);
        UserNotificationService notifications = new();
        List<UserNotification> published = [];
        notifications.Published += (_, eventArgs) => published.Add(eventArgs.Notification);
        var viewModel = Create(service, currentBeatmap, workspace, notifications);
        viewModel.UseMapComboColors = true;
        viewModel.Activate();

        // Act
        workspace.ClearSelection();

        // Assert
        currentBeatmap.FindCount.Should().Be(0);
        service.ColorPaths.Should().ContainSingle().Which.Should().Be("selected.osu");
        viewModel.AvailableColors.Should().BeEmpty();
        published.Should().BeEmpty();
    }

    private static SliderPicturatorViewModel Create(
        RecordingPicturator service,
        RecordingCurrentBeatmapLocator currentBeatmap,
        TestBeatmapWorkspace? workspace = null,
        UserNotificationService? notifications = null)
    {
        notifications ??= new UserNotificationService();
        DesktopApplicationSettings settings = new();
        return new SliderPicturatorViewModel(
            service,
            new StubImageFileService(),
            new TestFilePicker(),
            new ToolExecutionService(
                notifications,
                new RecordingEditorReloadService(),
                settings,
                TimeProvider.System),
            currentBeatmap,
            workspace ?? new TestBeatmapWorkspace(),
            settings,
            notifications);
    }

    private sealed class RecordingPicturator : ISliderPicturatorService
    {
        public long ResultSegmentCount { get; init; }

        public List<string> ColorPaths { get; } = [];

        public IReadOnlyList<RgbaColour> AvailableColors { get; init; } = [];

        public Task<SliderPicturatorResult> PicturateAsync(
            string path,
            SliderPicturatorServiceOptions options,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new SliderPicturatorResult(path, ResultSegmentCount));
        }

        public Task<IReadOnlyList<RgbaColour>> GetAvailableColorsAsync(
            string path,
            CancellationToken cancellationToken = default)
        {
            ColorPaths.Add(path);
            return Task.FromResult(AvailableColors);
        }

        public Task<HitObject?> GetSelectedSliderAsync(
            string path,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<HitObject?>(null);
        }
    }

    private sealed class StubImageFileService : IImageFileService
    {
        public Task<RgbaImage> LoadAsync(string path, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
