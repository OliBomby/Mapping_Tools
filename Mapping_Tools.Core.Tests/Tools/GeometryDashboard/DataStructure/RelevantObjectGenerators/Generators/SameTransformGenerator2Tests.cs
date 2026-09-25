using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Generators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Generators;

[TestClass]
public sealed class SameTransformGenerator2Tests
{
    [TestMethod]
    public void GetRelevantObjects_WithDiagonalStep_RepeatsSameDisplacement()
    {
        // Arrange
        SameTransformGenerator2 generator = new();
        RelevantPoint first = new(new Vector2(2, 3));
        RelevantPoint second = new(new Vector2(5, 7));

        // Act
        RelevantPoint? next = generator.GetRelevantObjects(first, second);

        // Assert
        next.Should().NotBeNull();
        next.Child.Should().Be(new Vector2(8, 11));
    }

    [TestMethod]
    public void GetRelevantObjects_WithRepeatedPoints_ReturnsNoProjection()
    {
        // Arrange
        SameTransformGenerator2 generator = new();
        RelevantPoint first = new(new Vector2(2, 3));
        RelevantPoint second = new(new Vector2(2, 3));

        // Act
        RelevantPoint? next = generator.GetRelevantObjects(first, second);

        // Assert
        next.Should().BeNull();
    }
}
