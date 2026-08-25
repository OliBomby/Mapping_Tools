using Mapping_Tools.Core.Graph;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.TumourGenerating.Models;
using Mapping_Tools.Core.Tools.TumourGenerating.Templates;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Tools.TumourGenerating;

[TestClass]
public sealed class TumourTemplateTests
{
    [TestMethod]
    public void Template_AllShapes_ExposeFiniteGeometryAndExpectedParameterContract()
    {
        // Arrange
        var templates = Enum.GetValues<TumourTemplate>();

        // Act
        var configured = templates.ToDictionary(
            templateKind => templateKind,
            templateKind =>
            {
                var layer = TumourLayer.GetDefaultLayer();
                layer.TumourTemplateEnum = templateKind;
                var template = layer.TumourTemplate;
                template.Length = 100;
                template.Width = 20;
                template.Parameter = 10;
                if (template is IRequireInit initializable) initializable.Init();

                return template;
            });

        // Assert
        configured.Should().HaveCount(4);
        configured.Values.Should().OnlyContain(template =>
            double.IsFinite(template.GetLength()) && double.IsFinite(template.GetOffset(0.5).Y));
        configured[TumourTemplate.Square].NeedsParameter.Should().BeTrue();
        configured[TumourTemplate.Circle].NeedsParameter.Should().BeFalse();
        configured[TumourTemplate.Triangle].NeedsParameter.Should().BeFalse();
        configured[TumourTemplate.Parabola].NeedsParameter.Should().BeFalse();
    }

    [TestMethod]
    public void Parabola_DistanceRelation_MapsEndpointsAndMidpointToNormalizedDistance()
    {
        // Arrange
        var layer = TumourLayer.GetDefaultLayer();
        layer.TumourTemplateEnum = TumourTemplate.Parabola;
        var template = layer.TumourTemplate;
        template.Length = 1;
        template.Width = 1;
        var distance = template.GetDistanceRelation()!;

        // Act
        double start = distance(0);
        double middle = distance(0.5);
        double end = distance(1);

        // Assert
        start.Should().BeApproximately(0, 1e-12);
        middle.Should().BeApproximately(0.5, 1e-12);
        end.Should().BeApproximately(1, 1e-12);
    }

    [TestMethod]
    public void Layer_Copy_PreservesGraphParametersAndPlacementRulesIndependently()
    {
        // Arrange
        var original = TumourLayer.GetDefaultLayer();
        original.TumourSidedness = TumourSidedness.AlternatingRight;
        original.WrappingMode = WrappingMode.Absolute;
        original.TumourParameter = TumourLayer.GetGraphState(42);
        original.TumourStart = -10;

        // Act
        var copy = original.Copy();
        copy.TumourParameter.Anchors[0].Pos = new Vector2(0, 99);
        copy.TumourStart = 5;

        // Assert
        copy.TumourSidedness.Should().Be(TumourSidedness.AlternatingRight);
        copy.WrappingMode.Should().Be(WrappingMode.Absolute);
        original.TumourParameter.GetValue(0).Should().Be(42);
        original.TumourStart.Should().Be(-10);
    }

    [TestMethod]
    public void SquareTemplate_ParameterGraphValue_ChangesTheShapeMargin()
    {
        // Arrange
        var first = CreateSquare(1);
        var second = CreateSquare(20);

        // Act
        double firstEdge = first.Template.GetOffset(0.1).Y;
        double secondEdge = second.Template.GetOffset(0.1).Y;

        // Assert
        first.Graph.GetValue(0).Should().Be(1);
        second.Graph.GetValue(0).Should().Be(20);
        firstEdge.Should().NotBe(secondEdge);
    }

    private static SquareTemplateValues CreateSquare(double parameter)
    {
        var layer = TumourLayer.GetDefaultLayer();
        layer.TumourTemplateEnum = TumourTemplate.Square;
        layer.TumourParameter = TumourLayer.GetGraphState(parameter);
        var template = layer.TumourTemplate;
        template.Length = 100;
        template.Width = 20;
        template.Parameter = layer.TumourParameter.GetValue(0);
        ((IRequireInit)template).Init();
        return new SquareTemplateValues(layer.TumourParameter, template);
    }

    private sealed record SquareTemplateValues(
        GraphState Graph,
        ITumourTemplate Template);
}
