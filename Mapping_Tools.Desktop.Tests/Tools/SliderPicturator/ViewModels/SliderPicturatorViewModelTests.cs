using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tools.SliderPicturator;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.Images;
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
        IShellProjectFeature feature = viewModel;

        // Act
        feature.Install(project);

        // Assert
        viewModel.SelectedSlider.Should().NotBeNull();
        viewModel.SelectedSlider!.Line.Should().Be(selectedSlider.Line);
    }

    private static SliderPicturatorViewModel Create(
        RecordingPicturator service,
        RecordingCurrentBeatmapLocator currentBeatmap)
    {
        UserNotificationService notifications = new();
        ApplicationSettings settings = new();
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
            new TestBeatmapWorkspace(),
            settings,
            notifications);
    }

    private sealed class RecordingPicturator : ISliderPicturatorService
    {
        public long ResultSegmentCount { get; init; }

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
            return Task.FromResult<IReadOnlyList<RgbaColour>>([]);
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
