using Mapping_Tools.Core.HitsoundStuff;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.Tools.HitsoundStudio.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Tools.HitsoundStudio.ViewModels;

[TestClass]
public sealed class HitsoundStudioImportDialogViewModelTests
{
    [TestMethod]
    public void Constructor_UsesSelectedMapsForLegacyImportDefaults()
    {
        // Arrange
        TestBeatmapWorkspace workspace = new();
        workspace.SetSelection(["first.osu", "second.osu"]);

        // Act
        HitsoundStudioImportDialogViewModel viewModel = new(
            "Layer 1",
            new TestCurrentBeatmapDialogService(),
            workspace,
            new TestFilePicker());

        // Assert
        viewModel.BeatmapPath.Should().Be("first.osu");
    }

    [TestMethod]
    public async Task PickSourceCommand_WithBeatmapImport_UsesSharedBeatmapPickerLocation()
    {
        // Arrange
        TestFilePicker filePicker = new() { OpenFiles = ["source.osu"] };
        HitsoundStudioImportDialogViewModel viewModel = new(
            "Layer 1",
            new TestCurrentBeatmapDialogService(),
            new TestBeatmapWorkspace
            {
                BeatmapPickerStartLocation = @"C:\Maps",
            },
            filePicker)
        {
            ImportType = ImportType.Hitsounds,
        };

        // Act
        await viewModel.PickSourceCommand.ExecuteAsync(null);

        // Assert
        viewModel.BeatmapPath.Should().Be("source.osu");
        filePicker.LastOpenRequest!.SuggestedStartLocation.Should().Be(@"C:\Maps");
    }

    [TestMethod]
    public async Task PickSourceCommand_WithMidiImport_LeavesStartLocationUnspecified()
    {
        // Arrange
        TestFilePicker filePicker = new() { OpenFiles = ["source.mid"] };
        HitsoundStudioImportDialogViewModel viewModel = new(
            "Layer 1",
            new TestCurrentBeatmapDialogService(),
            new TestBeatmapWorkspace
            {
                BeatmapPickerStartLocation = @"C:\Maps",
            },
            filePicker)
        {
            ImportType = ImportType.MIDI,
        };

        // Act
        await viewModel.PickSourceCommand.ExecuteAsync(null);

        // Assert
        viewModel.MidiPath.Should().Be("source.mid");
        filePicker.LastOpenRequest!.SuggestedStartLocation.Should().BeNull();
    }

    [TestMethod]
    public async Task LoadSourceCommand_UpdatesBeatmapPath()
    {
        // Arrange
        TestCurrentBeatmapDialogService currentBeatmap = new() { Path = "current.osu" };
        HitsoundStudioImportDialogViewModel viewModel = new(
            "Layer 1",
            currentBeatmap,
            new TestBeatmapWorkspace(),
            new TestFilePicker())
        {
            ImportType = ImportType.Stack,
            BeatmapPath = "selected.osu",
        };

        // Act
        await viewModel.LoadSourceCommand.ExecuteAsync(null);

        // Assert
        currentBeatmap.FetchCount.Should().Be(1);
        viewModel.BeatmapPath.Should().Be("current.osu");
    }
}
