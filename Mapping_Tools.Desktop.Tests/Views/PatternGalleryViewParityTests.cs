using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Views;

[TestClass]
public sealed class PatternGalleryViewParityTests
{
    [TestMethod]
    public void View_PreservesLegacyListSelectionProjectMenuAndActionContracts()
    {
        // Arrange
        string axaml = TestSourceReader.Read("Mapping_Tools.Desktop/Views/PatternGalleryView.axaml");
        string viewModel = TestSourceReader.Read("Mapping_Tools.Desktop/ViewModels/PatternGalleryViewModel.cs");

        // Assert
        axaml.Should().Contain("<ListBox ItemsSource=\"{Binding Patterns}\"");
        axaml.Should().Contain("SelectionMode=\"Single\"");
        axaml.Should().Contain("SelectionChanged=\"PatternSelectionChanged\"");
        axaml.Should().Contain("Import and export patterns from osu! beatmaps");
        axaml.Should().Contain("Delete selected patterns. Hold shift to skip dialog.");
        axaml.Should().NotContain("Select all");
        axaml.Should().NotContain("Clear selection");
        viewModel.Should().Contain("IShellExtraProjectMenuFeature");
        viewModel.Should().Contain("_Import collection");
        viewModel.Should().Contain("_Restore collection");
    }
}
