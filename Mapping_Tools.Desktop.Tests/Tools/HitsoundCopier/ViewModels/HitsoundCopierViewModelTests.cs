using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Tools.HitsoundCopier;
using Mapping_Tools.Application.Workspace.Models;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.Tools.HitsoundCopier.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Tools.HitsoundCopier.ViewModels;

[TestClass]
public sealed class HitsoundCopierViewModelTests
{
    [TestMethod]
    public async Task ImportBrowseCommand_WithCurrentFolderEnabled_UsesSelectedBeatmapDirectory()
    {
        // Arrange
        TestFilePicker picker = new() { OpenFiles = ["source.osu"] };
        TestBeatmapWorkspace workspace = new();
        workspace.SetSelection([@"C:\Maps\selected.osu"], BeatmapSelectionSource.FilePicker);
        workspace.BeatmapPickerStartLocation = @"C:\Maps";
        var viewModel = Create(picker, workspace);
        viewModel.PathFrom = @"E:\Other\source.osu";

        // Act
        await viewModel.ImportBrowseCommand.ExecuteAsync(null);

        // Assert
        picker.LastOpenRequest.Should().NotBeNull();
        picker.LastOpenRequest!.SuggestedStartLocation.Should().Be(@"C:\Maps");
    }

    [TestMethod]
    public async Task ExportBrowseCommand_WithSourcePath_UsesSourceBeatmapDirectory()
    {
        // Arrange
        TestFilePicker picker = new() { OpenFiles = ["target.osu"] };
        TestBeatmapWorkspace workspace = new();
        workspace.SetSelection([@"C:\Maps\selected.osu"], BeatmapSelectionSource.FilePicker);
        workspace.BeatmapPickerStartLocation = @"C:\Maps";
        var viewModel = Create(picker, workspace);
        viewModel.PathFrom = @"E:\Other\source.osu";

        // Act
        await viewModel.ExportBrowseCommand.ExecuteAsync(null);

        // Assert
        picker.LastOpenRequest.Should().NotBeNull();
        picker.LastOpenRequest!.SuggestedStartLocation.Should().Be(@"E:\Other");
    }

    [TestMethod]
    public async Task ImportBrowseCommand_WithCurrentFolderDisabled_LeavesStartLocationUnspecified()
    {
        // Arrange
        TestFilePicker picker = new() { OpenFiles = ["source.osu"] };
        TestBeatmapWorkspace workspace = new();
        workspace.SetSelection([@"C:\Maps\selected.osu"], BeatmapSelectionSource.FilePicker);
        workspace.BeatmapPickerStartLocation = null;
        var viewModel = Create(picker, workspace);

        // Act
        await viewModel.ImportBrowseCommand.ExecuteAsync(null);

        // Assert
        picker.LastOpenRequest.Should().NotBeNull();
        picker.LastOpenRequest!.SuggestedStartLocation.Should().BeNull();
    }

    private static HitsoundCopierViewModel Create(
        TestFilePicker filePicker,
        TestBeatmapWorkspace workspace)
    {
        return new HitsoundCopierViewModel(
            new TestHitsoundCopier(),
            new ToolExecutionService(
                new UserNotificationService(),
                TimeProvider.System),
            filePicker,
            new RecordingCurrentBeatmapLocator(),
            workspace,
            new UserNotificationService());
    }

    private sealed class TestHitsoundCopier : IHitsoundCopierService
    {
        public Task<HitsoundCopierResult> CopyAsync(
            HitsoundCopierServiceOptions options,
            bool quickRun = false,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
