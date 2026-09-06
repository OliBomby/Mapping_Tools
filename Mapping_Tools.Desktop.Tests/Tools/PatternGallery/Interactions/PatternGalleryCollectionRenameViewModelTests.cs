using Mapping_Tools.Desktop.Tools.PatternGallery.Interactions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Tools.PatternGallery.Interactions;

[TestClass]
public sealed class PatternGalleryCollectionRenameViewModelTests
{
    [TestMethod]
    public void AcceptCommand_WithCollectionAndDirectoryNames_ReturnsBothValues()
    {
        // Arrange
        PatternGalleryCollectionRenameViewModel viewModel = new("Collection", "Directory");
        object? result = null;
        viewModel.Close = value => result = value;
        viewModel.NewName = "Renamed collection";
        viewModel.NewFolderName = "renamed-directory";

        // Act
        viewModel.AcceptCommand.Execute(null);

        // Assert
        var input = result.Should().BeOfType<PatternGalleryCollectionRenameInput>().Subject;
        input.NewName.Should().Be("Renamed collection");
        input.NewFolderName.Should().Be("renamed-directory");
        viewModel.Error.Should().BeEmpty();
    }

    [TestMethod]
    public void AcceptCommand_WithBlankDirectoryName_LeavesDialogOpenAndExposesError()
    {
        // Arrange
        PatternGalleryCollectionRenameViewModel viewModel = new("Collection", "Directory");
        object? result = null;
        viewModel.Close = value => result = value;
        viewModel.NewFolderName = " ";

        // Act
        viewModel.AcceptCommand.Execute(null);

        // Assert
        result.Should().BeNull();
        viewModel.Error.Should().Be("A collection directory name is required.");
    }

    [TestMethod]
    public void CancelCommand_DismissesWithoutResult()
    {
        // Arrange
        PatternGalleryCollectionRenameViewModel viewModel = new("Collection", "Directory");
        object? result = new object();
        viewModel.Close = value => result = value;

        // Act
        viewModel.CancelCommand.Execute(null);

        // Assert
        result.Should().BeNull();
    }
}
