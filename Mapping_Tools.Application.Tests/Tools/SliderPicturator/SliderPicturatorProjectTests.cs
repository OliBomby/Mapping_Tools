using Mapping_Tools.Application.Tools.SliderPicturator;
using Mapping_Tools.Core.Tools.SliderPicturator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.Tools.SliderPicturator;

[TestClass]
public sealed class SliderPicturatorValidationTests
{
    [TestMethod]
    public void Validate_QualityOutsideLegacySliderRange_ThrowsValidationException()
    {
        // Arrange
        SliderPicturatorServiceOptions project = new() { PictureFile = "image.png", Quality = 102 };

        // Act
        Action act = () => SliderPicturatorEngine.Validate(project);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
