using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Desktop.Tools.TumourGenerator.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Tools.TumourGenerator.Controls;

[TestClass]
public sealed class ValueOrGraphControlTests
{
    [TestMethod]
    public void DefaultGraphState_IsIndependentForEachControl()
    {
        // Arrange
        ValueOrGraphControl first = new();
        ValueOrGraphControl second = new();

        // Act
        first.GraphState!.Anchors[0].Pos = new Vector2(5, 5);

        // Assert
        second.GraphState!.Anchors[0].Pos.Should().Be(new Vector2(0, 0));
    }
}
