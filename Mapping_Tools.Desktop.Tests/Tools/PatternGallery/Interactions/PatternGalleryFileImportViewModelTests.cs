using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.Tools.PatternGallery.Interactions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Tools.PatternGallery.Interactions;

[TestClass]
public sealed class PatternGalleryFileImportViewModelTests
{
    [TestMethod]
    public void AcceptCommand_WithOptionalTimeValues_ReturnsTypedInput()
    {
        // Arrange
        PatternGalleryFileImportViewModel viewModel = CreateViewModel("pattern.osu");
        object? result = null;
        viewModel.Close = value => result = value;
        viewModel.StartTime = -1;
        viewModel.EndTime = 1500;

        // Act
        viewModel.AcceptCommand.Execute(null);

        // Assert
        var input = result.Should().BeOfType<PatternGalleryFileInput>().Subject;
        input.Name.Should().Be("Pattern");
        input.FilePath.Should().Be("pattern.osu");
        input.StartTime.Should().Be(-1);
        input.EndTime.Should().Be(1500);
    }

    [TestMethod]
    public void AcceptCommand_WithBlankFilePath_LeavesDialogOpenAndReportsPathValidation()
    {
        // Arrange
        PatternGalleryFileImportViewModel viewModel = CreateViewModel(string.Empty);
        object? result = null;
        viewModel.Close = value => result = value;

        // Act
        viewModel.AcceptCommand.Execute(null);

        // Assert
        result.Should().BeNull();
        viewModel.GetErrors(nameof(PatternGalleryFileImportViewModel.FilePath))
            .Select(error => error.ErrorMessage)
            .Should()
            .Equal("A pattern file path is required.");
    }

    [TestMethod]
    public async Task BrowseCommand_WithBeatmapSelection_UpdatesFilePath()
    {
        // Arrange
        TestFilePicker filePicker = new() { OpenFiles = ["selected.osu"] };
        PatternGalleryFileImportViewModel viewModel = CreateViewModel(
            "initial.osu",
            filePicker);

        // Act
        await viewModel.BrowseCommand.ExecuteAsync(null);

        // Assert
        viewModel.FilePath.Should().Be("selected.osu");
        filePicker.LastOpenRequest!.Filters.Should().ContainSingle();
    }

    [TestMethod]
    public async Task UseCurrentCommand_WithAvailableBeatmap_UpdatesFilePath()
    {
        // Arrange
        var currentBeatmap = new RecordingCurrentBeatmapLocator("current.osu");
        PatternGalleryFileImportViewModel viewModel = CreateViewModel(
            "initial.osu",
            currentBeatmap: currentBeatmap);

        // Act
        await viewModel.UseCurrentCommand.ExecuteAsync(null);

        // Assert
        viewModel.FilePath.Should().Be("current.osu");
        currentBeatmap.FindCount.Should().Be(1);
    }

    private static PatternGalleryFileImportViewModel CreateViewModel(
        string path,
        TestFilePicker? filePicker = null,
        RecordingCurrentBeatmapLocator? currentBeatmap = null)
    {
        return new PatternGalleryFileImportViewModel(
            "Pattern",
            path,
            filePicker ?? new TestFilePicker(),
            currentBeatmap ?? new RecordingCurrentBeatmapLocator());
    }
}
