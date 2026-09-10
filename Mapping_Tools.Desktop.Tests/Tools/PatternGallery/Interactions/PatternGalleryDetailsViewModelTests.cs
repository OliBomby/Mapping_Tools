using Mapping_Tools.Core.Tools.PatternGallery.Models;
using Mapping_Tools.Desktop.Tools.PatternGallery.Interactions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Tools.PatternGallery.Interactions;

[TestClass]
public sealed class PatternGalleryDetailsViewModelTests
{
    [TestMethod]
    public void AcceptCommand_WithValidName_PreservesPatternMetadataAndReturnsName()
    {
        // Arrange
        PatternGalleryPattern pattern = new()
        {
            Name = "Original",
            CreationTime = new DateTime(2024, 1, 2, 3, 4, 5),
            LastUsedTime = new DateTime(2024, 2, 3, 4, 5, 6),
            UseCount = 2,
            ObjectCount = 4,
            Duration = TimeSpan.FromMilliseconds(1500),
            BeatLength = 375,
            FileName = "pattern.osu",
        };
        PatternGalleryDetailsViewModel viewModel = new(pattern);
        object? result = null;
        viewModel.Close = value => result = value;
        viewModel.Name = "Renamed";

        // Act
        viewModel.AcceptCommand.Execute(null);

        // Assert
        result.Should().Be("Renamed");
        viewModel.ObjectCountText.Should().Be("4");
        viewModel.FileName.Should().Be("pattern.osu");
    }

    [TestMethod]
    public void AcceptCommand_WithBlankName_LeavesDialogOpenAndReportsNameValidation()
    {
        // Arrange
        PatternGalleryDetailsViewModel viewModel = new(new PatternGalleryPattern
        {
            Name = "Original",
        });
        object? result = null;
        viewModel.Close = value => result = value;
        viewModel.Name = string.Empty;

        // Act
        viewModel.AcceptCommand.Execute(null);

        // Assert
        result.Should().BeNull();
        viewModel.GetErrors(nameof(PatternGalleryDetailsViewModel.Name))
            .Select(error => error.ErrorMessage)
            .Should()
            .Equal("A pattern name is required.");
    }
}
