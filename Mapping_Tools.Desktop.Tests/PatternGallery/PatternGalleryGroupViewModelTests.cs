using Mapping_Tools.Core.Tools.PatternGallery;
using Mapping_Tools.Desktop.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.PatternGallery;

[TestClass]
public sealed class PatternGalleryGroupViewModelTests
{
    [TestMethod]
    public void PatternGalleryGroupViewModel_WithItems_ExposesDisplayNameAndCount()
    {
        // Arrange
        PatternGalleryItemViewModel[] items =
        [
            new PatternGalleryItemViewModel(new PatternGalleryPattern { Name = "A" }),
            new PatternGalleryItemViewModel(new PatternGalleryPattern { Name = "B" })
        ];

        // Act
        PatternGalleryGroupViewModel group = new("None", items);

        // Assert
        group.Name.Should().Be("None");
        group.Patterns.Should().Equal(items);
        group.ItemCount.Should().Be(2);
    }

    [TestMethod]
    public void PatternGalleryItemViewModel_IsSelected_StaysInPresentationState()
    {
        // Arrange
        PatternGalleryPattern pattern = new();
        PatternGalleryItemViewModel item = new(pattern);

        // Act
        item.IsSelected = true;

        // Assert
        pattern.Name.Should().BeEmpty();
        item.IsSelected.Should().BeTrue();
    }
}
