using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tools.TimingCopier;
using Mapping_Tools.Core.Tools.TimingCopier.Models;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.Tools.TimingCopier.ViewModels;
using Mapping_Tools.Desktop.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Tools.TimingCopier.ViewModels;

[TestClass]
public sealed class TimingCopierViewModelTests
{
    [TestMethod]
    public void ExportPath_WithMultipleTargets_ReportsLegacyMapCount()
    {
        // Arrange
        var viewModel = Create();

        // Act
        viewModel.ExportPath = "first.osu|second.osu";

        // Assert
        viewModel.ExportMapCountText.Should().Be("(2) maps total");
    }

    [TestMethod]
    public async Task ExportBrowseCommand_WithMultipleFiles_UpdatesTargetPathsAndPickerRequest()
    {
        // Arrange
        TestFilePicker picker = new() { OpenFiles = ["first.osu", "second.osu"] };
        var viewModel = Create(filePicker: picker);
        viewModel.ImportPath = @"C:\maps\source.osu";

        // Act
        await viewModel.ExportBrowseCommand.ExecuteAsync(null);

        // Assert
        viewModel.ExportPath.Should().Be("first.osu|second.osu");
        picker.LastOpenRequest.Should().NotBeNull();
        picker.LastOpenRequest!.AllowMultiple.Should().BeTrue();
        picker.LastOpenRequest.SuggestedStartLocation.Should().Be(@"C:\maps");
    }

    [TestMethod]
    public async Task RunCommand_WithConfiguredPaths_PassesSnapshotAndResetsProgress()
    {
        // Arrange
        RecordingTimingCopier service = new();
        var viewModel = Create(service);
        viewModel.ImportPath = "source.osu";
        viewModel.ExportPath = "first.osu|second.osu";
        viewModel.ResnapMode = TimingCopierResnapMode.Resnap;

        // Act
        await viewModel.RunCommand.ExecuteAsync(null);

        // Assert
        service.Options.Should().NotBeNull();
        service.Options!.ImportPath.Should().Be("source.osu");
        service.Options.ExportPath.Should().Be("first.osu|second.osu");
        service.Options.ResnapMode.Should().Be(TimingCopierResnapMode.Resnap);
        viewModel.Progress.Should().Be(0);
        viewModel.IsRunning.Should().BeFalse();
    }

    [TestMethod]
    public void ResnapModes_ReturnAllCoreModesInEnumOrder()
    {
        // Arrange
        var viewModel = Create();

        // Act
        var modes = viewModel.ResnapModes;

        // Assert
        modes.Should().Equal(
            TimingCopierResnapMode.PreserveBeatSpacing,
            TimingCopierResnapMode.Resnap,
            TimingCopierResnapMode.KeepObjectsFixed);
    }

    private static TimingCopierViewModel Create(
        RecordingTimingCopier? service = null,
        TestFilePicker? filePicker = null)
    {
        return new TimingCopierViewModel(
            service ?? new RecordingTimingCopier(),
            new ToolExecutionService(
                new UserNotificationService(),
                new RecordingEditorReloadService(),
                new ApplicationSettings(),
                TimeProvider.System),
            filePicker ?? new TestFilePicker(),
            new RecordingCurrentBeatmapLocator(),
            new UserNotificationService(),
            new TestBeatmapWorkspace(),
            new ApplicationSettings());
    }

    private sealed class RecordingTimingCopier : ITimingCopierService
    {
        public TimingCopierServiceOptions? Options { get; private set; }

        public Task<TimingCopierResult> CopyAsync(
            TimingCopierServiceOptions options,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            Options = options;
            progress?.Report(1);
            return Task.FromResult(
                new TimingCopierResult(
                    options.ExportPath.Split('|', StringSplitOptions.RemoveEmptyEntries)));
        }
    }

}
