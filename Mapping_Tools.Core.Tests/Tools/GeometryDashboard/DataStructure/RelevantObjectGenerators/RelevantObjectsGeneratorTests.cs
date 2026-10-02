using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators;

[TestClass]
public sealed class RelevantObjectsGeneratorTests
{
    [TestMethod]
    public void Id_WithAllConcreteGenerators_IsNonEmptyAndUnique()
    {
        // Arrange
        var types = typeof(RelevantObjectsGenerator).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(RelevantObjectsGenerator).IsAssignableFrom(type));

        // Act
        string[] ids = types.Select(type => ((RelevantObjectsGenerator)Activator.CreateInstance(type)!).Id).ToArray();

        // Assert
        ids.Should().NotBeEmpty();
        ids.Should().AllSatisfy(id => id.Should().NotBeNullOrWhiteSpace());
        ids.Should().OnlyHaveUniqueItems();
    }
}
