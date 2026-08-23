using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.ComboColourStudio;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Application.Workspace;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.Tools.ComboColourStudio;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.ViewModels;

[TestClass]
public sealed class ComboColourStudioViewModelTests
{
    [TestMethod]
    public void AddColourPointCommand_AfterAddingPaletteColour_SelectsPointAndBuildsPreview()
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
        viewModel.PreviewItems.Should().ContainSingle();

        viewModel.ComboColours[0].Color = RgbaColour.FromRgb(1, 2, 3);
        viewModel.PreviewItems.Single().Colour.Should().Be(RgbaColour.FromRgb(1, 2, 3));
    }

    private static ComboColourStudioViewModel CreateViewModel()
    {
        return new ComboColourStudioViewModel(
            new StubComboColourStudioService(),
            new ToolExecutionService(
                new UserNotificationService(),
                new StubReloadService(),
                new ApplicationSettings(),
                TimeProvider.System),
            new TestBeatmapWorkspace(),
            new StubCurrentBeatmapLocator(),
            new StubLiveBeatmapReader(),
            new TestFilePicker());
    }

    private sealed class StubComboColourStudioService : IComboColourStudioService
    {
        public Task ImportComboColoursAsync(
            string path,
            ComboColourProject project,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task ImportColourHaxAsync(
            string path,
            ComboColourProject project,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<ComboColourStudioRunResult> ApplyAsync(
            IReadOnlyList<string> paths,
            ComboColourProject project,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ComboColourStudioRunResult(paths.Count));
        }
    }

    private sealed class StubCurrentBeatmapLocator : ICurrentBeatmapLocator
    {
        public Task<string?> FindCurrentBeatmapAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<string?>(null);
        }
    }

    private sealed class StubLiveBeatmapReader : ILiveBeatmapReader
    {
        public Task<LiveBeatmapSnapshot?> ReadAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<LiveBeatmapSnapshot?>(null);
        }
    }

    private sealed class StubReloadService : IEditorReloadService
    {
        public Task ReloadAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
