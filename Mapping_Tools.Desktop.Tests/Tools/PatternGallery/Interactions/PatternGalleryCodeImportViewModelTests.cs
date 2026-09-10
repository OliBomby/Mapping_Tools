using Mapping_Tools.Desktop.Tools.PatternGallery.Interactions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Tools.PatternGallery.Interactions;

[TestClass]
public sealed class PatternGalleryCodeImportViewModelTests
{
    [TestMethod]
    public void AcceptCommand_WithCodeValues_ReturnsTypedInput()
    {
        // Arrange
        PatternGalleryCodeImportViewModel viewModel = new("Pattern")
        {
            HitObjects = "1,2,3,4,5,1,0,0:0:0:0:",
            TimingPoints = "0,500",
            GlobalSv = 1.75,
        };
        object? result = null;
        viewModel.Close = value => result = value;

        // Act
        viewModel.AcceptCommand.Execute(null);

        // Assert
        var input = result.Should().BeOfType<PatternGalleryCodeInput>().Subject;
        input.Name.Should().Be("Pattern");
        input.HitObjects.Should().Be("1,2,3,4,5,1,0,0:0:0:0:");
        input.TimingPoints.Should().Be("0,500");
        input.GlobalSv.Should().Be(1.75);
    }

    [TestMethod]
    public void AcceptCommand_WithBlankName_LeavesDialogOpenAndReportsNameValidation()
    {
        // Arrange
        PatternGalleryCodeImportViewModel viewModel = new("Pattern");
        object? result = null;
        viewModel.Close = value => result = value;
        viewModel.Name = string.Empty;

        // Act
        viewModel.AcceptCommand.Execute(null);

        // Assert
        result.Should().BeNull();
        viewModel.GetErrors(nameof(PatternGalleryCodeImportViewModel.Name))
            .Select(error => error.ErrorMessage)
            .Should()
            .Equal("A pattern name is required.");
    }
}
