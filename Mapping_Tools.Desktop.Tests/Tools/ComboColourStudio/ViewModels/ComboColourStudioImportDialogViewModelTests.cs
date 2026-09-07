using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.Tools.ComboColourStudio.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Tools.ComboColourStudio.ViewModels;

[TestClass]
public sealed class ComboColourStudioImportDialogViewModelTests
{
    [TestMethod]
    public void AcceptCommand_WithBlankPath_LeavesDialogOpenAndReportsValidationError()
    {
        // Arrange
        ComboColourStudioImportDialogViewModel viewModel = CreateViewModel();
        object? result = null;
        viewModel.Close = value => result = value;
        viewModel.Path = "  ";

        // Act
        viewModel.AcceptCommand.Execute(null);

        // Assert
        result.Should().BeNull();
        viewModel.Error.Should().Be("A beatmap path is required.");
    }

    [TestMethod]
    public async Task BrowseCommand_WithBeatmapSelection_UpdatesPath()
    {
        // Arrange
        TestFilePicker filePicker = new() { OpenFiles = ["selected.osu"] };
        ComboColourStudioImportDialogViewModel viewModel =
            new("initial.osu", new RecordingCurrentBeatmapLocator(), filePicker);

        // Act
        await viewModel.BrowseCommand.ExecuteAsync(null);

        // Assert
        viewModel.Path.Should().Be("selected.osu");
        filePicker.LastOpenRequest!.Filters.Should().ContainSingle();
    }

    [TestMethod]
    public async Task UseCurrentCommand_WithAvailableBeatmap_UpdatesPath()
    {
        // Arrange
        var currentBeatmap = new RecordingCurrentBeatmapLocator("current.osu");
        ComboColourStudioImportDialogViewModel viewModel =
            new("initial.osu", currentBeatmap, new TestFilePicker());

        // Act
        await viewModel.UseCurrentCommand.ExecuteAsync(null);

        // Assert
        viewModel.Path.Should().Be("current.osu");
        currentBeatmap.FindCount.Should().Be(1);
    }

    private static ComboColourStudioImportDialogViewModel CreateViewModel()
    {
        return new ComboColourStudioImportDialogViewModel(
            string.Empty,
            new RecordingCurrentBeatmapLocator(),
            new TestFilePicker());
    }
}
