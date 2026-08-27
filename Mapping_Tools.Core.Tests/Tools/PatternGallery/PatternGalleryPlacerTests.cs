using Mapping_Tools.Core.Tools.PatternGallery;
using Mapping_Tools.Core.Tools.PatternGallery.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Tools.PatternGallery;

[TestClass]
public sealed class PatternGalleryPlacerTests
{
    [TestMethod]
    public void Validate_WithNonFinitePadding_ThrowsValidationException()
    {
        // Arrange
        PatternGalleryEngineOptions options = new()
        {
            Padding = double.PositiveInfinity,
        };

        // Act
        Action act = () => PatternGalleryPlacer.Validate(options);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*numeric*");
    }

    [TestMethod]
    public void Validate_WithDefaultOptions_DoesNotThrow()
    {
        // Arrange
        PatternGalleryEngineOptions options = new();

        // Act
        Action act = () => PatternGalleryPlacer.Validate(options);

        // Assert
        act.Should().NotThrow();
    }
}
