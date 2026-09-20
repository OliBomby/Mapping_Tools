using Mapping_Tools.Core.HitsoundStuff;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.Tools.HitsoundStudio.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Tools.HitsoundStudio.ViewModels;

[TestClass]
public sealed class HitsoundStudioImportDialogViewModelTests
{
    [TestMethod]
    public async Task PickSourceCommand_WithBeatmapImport_UsesSharedBeatmapPickerLocation()
    {
        // Arrange
        TestFilePicker filePicker = new() { OpenFiles = ["source.osu"] };
        HitsoundStudioImportDialogViewModel viewModel = new(
            "Layer 1",
            new RecordingCurrentBeatmapLocator(),
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
        viewModel.SourcePaths.Should().Be("source.osu");
        filePicker.LastOpenRequest!.SuggestedStartLocation.Should().Be(@"C:\Maps");
    }

    [TestMethod]
    public async Task PickSourceCommand_WithMidiImport_LeavesStartLocationUnspecified()
    {
        // Arrange
        TestFilePicker filePicker = new() { OpenFiles = ["source.mid"] };
        HitsoundStudioImportDialogViewModel viewModel = new(
            "Layer 1",
            new RecordingCurrentBeatmapLocator(),
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
}
