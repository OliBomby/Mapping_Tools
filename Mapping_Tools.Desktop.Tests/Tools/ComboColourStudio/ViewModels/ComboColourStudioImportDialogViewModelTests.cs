using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.Tools.ComboColourStudio.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Tools.ComboColourStudio.ViewModels;

[TestClass]
public sealed class ComboColourStudioImportDialogViewModelTests
{
    [TestMethod]
    public void AcceptCommand_WithBlankPath_LeavesDialogOpen()
    {
        // Arrange
        var viewModel = CreateViewModel();
        object? result = null;
        viewModel.Close = value => result = value;
        viewModel.Path = "  ";

        // Act
        viewModel.AcceptCommand.Execute(null);

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task BrowseCommand_WithBeatmapSelection_UpdatesPath()
    {
        // Arrange
        TestFilePicker filePicker = new() { OpenFiles = ["selected.osu"] };
        ComboColourStudioImportDialogViewModel viewModel =
            new("initial.osu", new TestCurrentBeatmapDialogService(), new TestBeatmapWorkspace
            {
                BeatmapPickerStartLocation = @"C:\Maps",
            }, filePicker);

        // Act
        await viewModel.BrowseCommand.ExecuteAsync(null);

        // Assert
        viewModel.Path.Should().Be("selected.osu");
        filePicker.LastOpenRequest!.Filters.Should().ContainSingle();
        filePicker.LastOpenRequest.SuggestedStartLocation.Should().Be(@"C:\Maps");
    }

    [TestMethod]
    public async Task UseCurrentCommand_WithAvailableBeatmap_UpdatesPath()
    {
        // Arrange
        var currentBeatmap = new TestCurrentBeatmapDialogService { Path = "current.osu" };
        ComboColourStudioImportDialogViewModel viewModel =
            new("initial.osu", currentBeatmap, new TestBeatmapWorkspace(), new TestFilePicker());

        // Act
        await viewModel.UseCurrentCommand.ExecuteAsync(null);

        // Assert
        viewModel.Path.Should().Be("current.osu");
        currentBeatmap.FetchCount.Should().Be(1);
    }

    private static ComboColourStudioImportDialogViewModel CreateViewModel()
    {
        return new ComboColourStudioImportDialogViewModel(
            string.Empty,
            new TestCurrentBeatmapDialogService(),
            new TestBeatmapWorkspace(),
            new TestFilePicker());
    }
}
