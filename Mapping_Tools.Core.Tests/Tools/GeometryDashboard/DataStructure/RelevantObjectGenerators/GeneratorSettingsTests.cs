using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators;

[TestClass]
public sealed class GeneratorSettingsTests
{
    [TestMethod]
    public void Id_WithAllConcreteSettings_IsNonEmptyAndUnique()
    {
        // Arrange
        var types = typeof(GeneratorSettings).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(GeneratorSettings).IsAssignableFrom(type));

        // Act
        string[] ids = types.Select(type => ((GeneratorSettings)Activator.CreateInstance(type)!).Id).ToArray();

        // Assert
        ids.Should().NotBeEmpty();
        ids.Should().AllSatisfy(id => id.Should().NotBeNullOrWhiteSpace());
        ids.Should().OnlyHaveUniqueItems();
    }
}
