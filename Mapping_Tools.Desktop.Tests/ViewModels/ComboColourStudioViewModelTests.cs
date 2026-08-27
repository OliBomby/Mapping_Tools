using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tools.ComboColourStudio;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.Tools.ComboColourStudio.Models;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.ViewModels;

[TestClass]
public sealed class ComboColourStudioViewModelTests
{
    [TestMethod]
    public void AddColourPointCommand_AfterAddingPaletteColour_SelectsPointAndAddsSequence()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.AddComboColourCommand.Execute(null);

        // Act
        ((IRelayCommand)viewModel.AddColourPointCommand).Execute(null);
        viewModel.SelectedSequenceColour = viewModel.ComboColours[0];
        viewModel.AddSequenceColourCommand.Execute(viewModel.SelectedColourPoint);

        // Assert
        viewModel.Project.ComboColours.Should().ContainSingle();
        viewModel.Project.ColourPoints.Should().ContainSingle();
        viewModel.SelectedColourPoint.Should().NotBeNull();
        viewModel.SelectedColourPoint!.Model.Time.Should().Be(viewModel.Project.ColourPoints[0].Time);
        viewModel.SelectedColourPoint.ColourSequence.Should().ContainSingle();
    }

    private static ComboColourStudioViewModel CreateViewModel()
    {
        return new ComboColourStudioViewModel(
            new StubComboColourStudioService(),
            new ToolExecutionService(
                new UserNotificationService(),
                new RecordingEditorReloadService(),
                new ApplicationSettings(),
                TimeProvider.System),
            new TestBeatmapWorkspace(),
            new RecordingCurrentBeatmapLocator(),
            new RecordingLiveBeatmapReader((LiveBeatmapSnapshot?)null),
            new TestFilePicker());
    }

    private sealed class StubComboColourStudioService : IComboColourStudioService
    {
        public Task<ComboColourEngineOptions> ImportComboColoursAsync(
            string path,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new ComboColourEngineOptions());

        public Task<ComboColourEngineOptions> ImportColourHaxAsync(
            string path,
            int maxBurstLength,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ComboColourEngineOptions { MaxBurstLength = maxBurstLength });
        }

        public Task<ComboColourStudioRunResult> ApplyAsync(
            IReadOnlyList<string> paths,
            ComboColourServiceOptions project,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ComboColourStudioRunResult(paths.Count));
        }
    }

}
