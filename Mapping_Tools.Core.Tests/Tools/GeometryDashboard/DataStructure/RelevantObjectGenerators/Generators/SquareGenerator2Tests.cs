using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Generators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Generators;

[TestClass]
public sealed class SquareGenerator2Tests
{
    [TestMethod]
    public void GetRelevantObjects_WithHorizontalSide_GeneratesBothSquareOrientations()
    {
        // Arrange
        SquareGenerator2 generator = new();
        RelevantPoint first = new(new Vector2(0, 0));
        RelevantPoint second = new(new Vector2(10, 0));

        // Act
        RelevantPoint[] points = generator.GetRelevantObjects(first, second);

        // Assert
        Vector2[] expected = [
            new Vector2(0, -10), new Vector2(0, 10),
            new Vector2(10, -10), new Vector2(10, 10),
        ];
        points.Should().HaveCount(expected.Length);
        for (int index = 0; index < expected.Length; index++)
        {
            points[index].Child.X.Should().BeApproximately(expected[index].X, 0.000001);
            points[index].Child.Y.Should().BeApproximately(expected[index].Y, 0.000001);
        }
    }
}
