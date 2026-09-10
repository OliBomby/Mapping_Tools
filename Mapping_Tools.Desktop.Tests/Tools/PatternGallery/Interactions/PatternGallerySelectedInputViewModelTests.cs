using Mapping_Tools.Desktop.Tools.PatternGallery.Interactions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Tools.PatternGallery.Interactions;

[TestClass]
public sealed class PatternGallerySelectedInputViewModelTests
{
    [TestMethod]
    public void AcceptCommand_WithBlankName_LeavesDialogOpenAndReportsNameValidation()
    {
        // Arrange
        PatternGallerySelectedInputViewModel viewModel = new("Pattern");
        object? result = null;
        viewModel.Close = value => result = value;
        viewModel.Name = string.Empty;

        // Act
        viewModel.AcceptCommand.Execute(null);

        // Assert
        result.Should().BeNull();
        viewModel.GetErrors(nameof(PatternGallerySelectedInputViewModel.Name))
            .Select(error => error.ErrorMessage)
            .Should()
            .Equal("A pattern name is required.");
    }
}
