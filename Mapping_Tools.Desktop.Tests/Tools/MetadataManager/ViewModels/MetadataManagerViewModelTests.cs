using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Desktop.Services.Undo;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.Tools.MetadataManager.Models;
using Mapping_Tools.Infrastructure.Projects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Tools.MetadataManager.ViewModels;

[TestClass]
public sealed class MetadataManagerViewModelTests
{
    [TestMethod]
    public void ComboColorPickerChange_Undo_RestoresPreviousColor()
    {
        // Arrange
        var viewModel = MetadataManagerViewModelTestFactory.Create();
        viewModel.AddComboColourCommand.Execute(null);
        var original = viewModel.ComboColours[0].Color;
        ProjectUndoHistory<MetadataManagerProject> history = new(viewModel, new VersionedProjectJsonSerializer());
        viewModel.UndoHistory = history;

        // Act
        viewModel.ComboColours[0].Color = RgbaColour.FromRgb(12, 34, 56);
        history.Capture();
        history.Undo();

        // Assert
        viewModel.ComboColours[0].Color.Should().Be(original);
        history.CanUndo.Should().BeFalse();
        history.Redo();
        viewModel.ComboColours[0].Color.Should().Be(RgbaColour.FromRgb(12, 34, 56));
    }

    [TestMethod]
    public async Task BrowseExportCommand_WithMultipleFiles_JoinsPathsAndRequestsMultiSelect()
    {
        // Arrange
        TestFilePicker picker = new() { OpenFiles = ["first.osu", "second.osu"] };
        TestBeatmapWorkspace workspace = new()
        {
            BeatmapPickerStartLocation = "Maps",
        };
        var viewModel = MetadataManagerViewModelTestFactory.Create(filePicker: picker, workspace: workspace);
        string importPath = Path.Combine("Source", "source.osu");
        viewModel.ImportPath = importPath;

        // Act
        await ExecuteAsync(viewModel.BrowseExportCommand);

        // Assert
        viewModel.ExportPath.Should().Be("first.osu|second.osu");
        viewModel.ExportMapCountText.Should().Be("(2) maps total");
        picker.LastOpenRequest.Should().NotBeNull();
        picker.LastOpenRequest!.AllowMultiple.Should().BeTrue();
        picker.LastOpenRequest.SuggestedStartLocation.Should().Be(Path.GetDirectoryName(importPath));
    }

    [TestMethod]
    public async Task ImportCommand_WithExistingExportPath_PreservesExportPath()
    {
        // Arrange
        var viewModel = MetadataManagerViewModelTestFactory.Create();
        viewModel.ImportPath = "source.osu";
        viewModel.ExportPath = "existing-target.osu";

        // Act
        await ExecuteAsync(viewModel.ImportCommand);

        // Assert
        viewModel.ImportPath.Should().Be("source.osu");
        viewModel.ExportPath.Should().Be("existing-target.osu");
    }

    [TestMethod]
    public async Task BrowseImportCommand_UsesSharedBeatmapPickerLocation()
    {
        // Arrange
        TestFilePicker picker = new() { OpenFiles = ["source.osu"] };
        TestBeatmapWorkspace workspace = new()
        {
            BeatmapPickerStartLocation = @"C:\Maps",
        };
        var viewModel = MetadataManagerViewModelTestFactory.Create(filePicker: picker, workspace: workspace);

        // Act
        await ExecuteAsync(viewModel.BrowseImportCommand);

        // Assert
        picker.LastOpenRequest!.SuggestedStartLocation.Should().Be(@"C:\Maps");
    }

    [TestMethod]
    public async Task RunCommand_WithConfiguredMetadata_ExecutesServiceAndResetsProgress()
    {
        // Arrange
        RecordingMetadataManagerService metadataManager = new();
        var viewModel = MetadataManagerViewModelTestFactory.Create(metadataManager);
        viewModel.ExportPath = "first.osu|second.osu";
        viewModel.Artist = "Wave Artist";
        viewModel.RomanisedArtist = "Wave Artist";
        viewModel.Title = "Wave Title";
        viewModel.RomanisedTitle = "Wave Title";
        viewModel.BeatmapCreator = "Mapper";

        // Act
        await ExecuteAsync(viewModel.RunCommand);

        // Assert
        metadataManager.Options.Should().NotBeNull();
        metadataManager.Options!.ExportPath.Should().Be("first.osu|second.osu");
        metadataManager.Options.Artist.Should().Be("Wave Artist");
        viewModel.Progress.Should().Be(0);
        viewModel.IsRunning.Should().BeFalse();
    }

    private static Task ExecuteAsync(IAsyncRelayCommand command)
    {
        return command.ExecuteAsync(null);
    }

}
