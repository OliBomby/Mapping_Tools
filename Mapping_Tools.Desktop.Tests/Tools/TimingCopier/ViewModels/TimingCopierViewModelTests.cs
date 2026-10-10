using Mapping_Tools.Application.Execution.ToolExecution;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Mapping_Tools.Desktop.Tests.TestHelpers;
using Mapping_Tools.Desktop.Tools.TimingCopier.Views;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Localization;
using Mapping_Tools.Application.Tools.TimingCopier;
using Mapping_Tools.Core.Tools.TimingCopier.Models;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.Shell;
using Mapping_Tools.Desktop.Tools.TimingCopier.Models;
using Mapping_Tools.Desktop.Tools.TimingCopier.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Tools.TimingCopier.ViewModels;

[TestClass]
[DoNotParallelize]
public sealed class TimingCopierViewModelTests
{
    [DataTestMethod]
    [DataRow(TimingCopierResnapMode.PreserveBeatSpacing, "Aantal beats tussen objecten blijft gelijk")]
    [DataRow(TimingCopierResnapMode.Resnap, "Alleen opnieuw snappen")]
    [DataRow(TimingCopierResnapMode.KeepObjectsFixed, "Objecten niet verplaatsen")]
    public void ResnapMode_LanguageChanges_RefreshesSelectedLabelWithoutChangingMode(
        TimingCopierResnapMode mode, string expectedLabel)
    {
        // Arrange
        string? previous = TranslationManager.Language;
        TranslationManager.SetLanguage("en");
        var viewModel = Create();
        viewModel.ResnapMode = mode;
        TimingCopierView view = new() { DataContext = viewModel };
        using var host = HeadlessViewHost.Show(view);
        ComboBox picker = view.GetVisualDescendants().OfType<ComboBox>().Single();

        try
        {
            // Act
            TranslationManager.SetLanguage("nl");
            HeadlessViewHost.RunDispatcherJobs();

            // Assert
            picker.GetVisualDescendants().OfType<TextBlock>().Where(block => block.IsEffectivelyVisible)
                .Select(block => block.Text).Should().Contain(expectedLabel);
            picker.SelectedItem.Should().Be(mode);
            viewModel.ResnapMode.Should().Be(mode);
        }
        finally
        {
            TranslationManager.SetLanguage(previous);
        }
    }

    [TestMethod]
    public void Constructor_DutchLanguage_PreservesProjectStorageIdentityAndTypedOptions()
    {
        // Arrange
        string? previous = TranslationManager.Language;

        try
        {
            TranslationManager.SetLanguage("nl");

            // Act
            var viewModel = Create();
            var definition = ((IShellProjectFeature<TimingCopierProject>)viewModel).ProjectDefinition;

            // Assert
            definition.ProjectFolderName.Should().Be("Timing Copier Projects");
            definition.AutoSaveFileName.Should().Be("timingcopierproject.json");
            definition.SuggestedFileName.Should().Be("timing-copier-project.json");
            viewModel.ResnapMode.Should().Be(TimingCopierResnapMode.PreserveBeatSpacing);
        }
        finally
        {
            TranslationManager.SetLanguage(previous);
        }
    }

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
        string importPath = Path.Combine("maps", "source.osu");
        viewModel.ImportPath = importPath;

        // Act
        await viewModel.ExportBrowseCommand.ExecuteAsync(null);

        // Assert
        viewModel.ExportPath.Should().Be("first.osu|second.osu");
        picker.LastOpenRequest.Should().NotBeNull();
        picker.LastOpenRequest!.AllowMultiple.Should().BeTrue();
        picker.LastOpenRequest.SuggestedStartLocation.Should().Be(Path.GetDirectoryName(importPath));
    }

    [TestMethod]
    public async Task ImportBrowseCommand_UsesSharedBeatmapPickerLocation()
    {
        // Arrange
        TestFilePicker picker = new() { OpenFiles = ["source.osu"] };
        TestBeatmapWorkspace workspace = new()
        {
            BeatmapPickerStartLocation = @"C:\Maps",
        };
        var viewModel = Create(filePicker: picker, workspace: workspace);

        // Act
        await viewModel.ImportBrowseCommand.ExecuteAsync(null);

        // Assert
        picker.LastOpenRequest!.SuggestedStartLocation.Should().Be(@"C:\Maps");
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
        TestFilePicker? filePicker = null,
        TestBeatmapWorkspace? workspace = null)
    {
        return new TimingCopierViewModel(
            service ?? new RecordingTimingCopier(),
            new ToolExecutionService(
                new UserNotificationService(),
                TimeProvider.System),
            filePicker ?? new TestFilePicker(),
            new TestCurrentBeatmapDialogService(),
            new UserNotificationService(),
            workspace ?? new TestBeatmapWorkspace());
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
