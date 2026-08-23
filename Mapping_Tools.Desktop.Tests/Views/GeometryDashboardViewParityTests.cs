using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Views;

[TestClass]
public sealed class GeometryDashboardViewParityTests
{
    [TestMethod]
    public void View_AgainstLegacyDashboardContract_PreservesActionsAndStateLabels()
    {
        // Arrange
        string source = TestSourceReader.Read("Mapping_Tools.Desktop/Views/GeometryDashboardView.axaml");

        // Act
        bool containsFeatureFooterPersistence = source.Contains("Save locked virtual objects", StringComparison.Ordinal)
                                                || source.Contains("Load locked virtual objects", StringComparison.Ordinal);

        // Assert
        source.Should().Contain("Geometry Dashboard");
        source.Should().Contain("Search for a generator.");
        source.Should().Contain("Toggle selection on all virtual objects");
        source.Should().Contain("Toggle locked on all virtual objects");
        source.Should().Contain("Toggle usability on all virtual objects");
        containsFeatureFooterPersistence.Should().BeFalse();
        source.Should().Contain("Generator settings...");
        source.Should().Contain("Made by OliBomby");
        source.Should().Contain("MappingToolsGeometryProgressBrush");
        source.Should().NotContain("Foreground=\"#", "view colors belong in the focused resource dictionary");
    }
}
