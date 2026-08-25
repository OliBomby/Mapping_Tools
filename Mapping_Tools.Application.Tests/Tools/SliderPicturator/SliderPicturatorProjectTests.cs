using Mapping_Tools.Application.Tools.SliderPicturator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.Tools.SliderPicturator;

[TestClass]
public sealed class SliderPicturatorProjectTests
{
    [TestMethod]
    public void Validate_QualityOutsideLegacySliderRange_ThrowsBeforeRun()
    {
        // Arrange
        SliderPicturatorProject project = new() { PictureFile = "image.png", Quality = 102 };

        // Act
        var act = project.Validate;

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
