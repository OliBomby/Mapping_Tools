using Mapping_Tools.Core.Classes.Graph;
using Mapping_Tools.Core.Classes.MathUtil;
using Mapping_Tools.Desktop.Converters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Converters;

[TestClass]
public sealed class GraphStateTextConverterTests
{
    [TestMethod]
    public void Convert_ConstantGraph_UsesScalarMode()
    {
        // Arrange
        GraphStateTextConverter converter = new();

        // Act
        object result = converter.Convert(GraphStateTextCodec.CreateConstant(2.5), typeof(string), null, System.Globalization.CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be("2.5");
    }

    [TestMethod]
    public void ConvertBack_CurveText_ReturnsIndependentGraphState()
    {
        // Arrange
        GraphStateTextConverter converter = new();
        GraphState source = new(
            [new GraphAnchor(new Vector2(0, 0)), new GraphAnchor(new Vector2(1, 1))],
            0,
            0,
            1,
            1);
        string text = GraphStateTextCodec.Format(source);

        // Act
        object result = converter.ConvertBack(text, typeof(GraphState), null, System.Globalization.CultureInfo.InvariantCulture);

        // Assert
        result.Should().BeOfType<GraphState>();
        ((GraphState)result).Should().NotBeSameAs(source);
    }

    [TestMethod]
    public void ConvertBack_EmptyText_ReturnsBindingValidationError()
    {
        // Arrange
        GraphStateTextConverter converter = new();

        // Act
        object result = converter.ConvertBack(string.Empty, typeof(GraphState), null, System.Globalization.CultureInfo.InvariantCulture);

        // Assert
        result.Should().BeOfType<Avalonia.Data.BindingNotification>();
        ((Avalonia.Data.BindingNotification)result).ErrorType.Should().Be(Avalonia.Data.BindingErrorType.DataValidationError);
    }
}
