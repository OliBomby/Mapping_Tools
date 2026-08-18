using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Views;

[TestClass]
public sealed class TumourGeneratorViewParityTests
{
    [TestMethod]
    public void View_PreservesLegacyCommandsAndTooltips_Expectation()
    {
        // Arrange
        string axaml = File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "Fixtures", "TumourGeneratorView.axaml"));

        // Act
        string[] requiredPresentationContracts =
        [
            "AddCommand",
            "RemoveCommand",
            "CopyCommand",
            "RaiseCommand",
            "LowerCommand",
            "ImportCommand",
            "Preview slider",
            "Slider selection mode. Choose which sliders should be targeted by Tumour Generator 2.",
            "Randomize the random seed.",
            "Result preview",
            "ValueOrGraphControl",
            "ObjectVisualiserControl",
            "CircleSizeToThicknessConverter",
            "Thickness=\"{Binding CircleSize",
            "ColumnDefinitions=\"200,Auto,200,Auto,*\"",
            "SmallChange=\"0.1\""
        ];

        // Assert
        requiredPresentationContracts.Should().OnlyContain(
            contract => axaml.Contains(contract, StringComparison.Ordinal));
        axaml.Should().NotContain("Height=\"200\"", "the WPF layer list fills the available left-panel row");
    }
}
