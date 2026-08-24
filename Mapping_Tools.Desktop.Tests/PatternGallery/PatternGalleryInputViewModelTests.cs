using Mapping_Tools.Core.Tools.PatternGallery;
using Mapping_Tools.Core.Tools.PatternGallery.Models;
using Mapping_Tools.Desktop.Interactions;
using Mapping_Tools.Desktop.Interactions.PatternGallery;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PatternGalleryDetailsViewModel = Mapping_Tools.Desktop.Interactions.PatternGallery.PatternGalleryDetailsViewModel;
using PatternGalleryInputViewModel = Mapping_Tools.Desktop.Interactions.PatternGallery.PatternGalleryInputViewModel;

namespace Mapping_Tools.Desktop.Tests.PatternGallery;

[TestClass]
public sealed class PatternGalleryInputViewModelTests
{
    [TestMethod]
    public void ForFile_Accept_WithBlankAndOsuTimestampBounds_UsesLegacyOptionalTimeValues()
    {
        // Arrange
        var viewModel = PatternGalleryInputViewModel.ForFile("Pattern", "pattern.osu");
        object? result = null;
        viewModel.Close = value => result = value;
        viewModel.StartTimeText = string.Empty;
        viewModel.EndTimeText = "00:01:500";

        // Act
        viewModel.AcceptCommand.Execute(null);

        // Assert
        var input = result.Should().BeOfType<PatternGalleryFileInput>().Subject;
        input.StartTime.Should().Be(-1);
        input.EndTime.Should().Be(1500);
    }

    [TestMethod]
    public void DetailsViewModel_Accept_PreservesPatternMetadataForDisplayAndReturnsName()
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
}
