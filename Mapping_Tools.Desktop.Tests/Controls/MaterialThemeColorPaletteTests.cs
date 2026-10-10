using Avalonia.Media;
using Mapping_Tools.Desktop.Controls;
using Mapping_Tools.Desktop.Models;
using Material.Colors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Controls;

[TestClass]
public sealed class MaterialThemeColorPaletteTests
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void GetColor_WithThemePalette_OffersEveryOriginalChoiceAndDefaultFirst(bool accent)
    {
        // Arrange
        var palette = accent ? MaterialThemeColorPalette.Accent : MaterialThemeColorPalette.Primary;
        var settings = new DesktopApplicationSettings();
        var defaultColor = Color.Parse(accent ? settings.AccentColor : settings.PrimaryColor);
        var choices = accent
            ? Enum.GetValues<SecondaryColor>().Select(choice => (MaterialColor)choice)
            : Enum.GetValues<PrimaryColor>().Select(choice => (MaterialColor)choice);
        var expected = choices.Select(choice => SwatchHelper.Lookup[choice]).ToArray();

        // Act
        var colors = Enumerable.Range(0, palette.ShadeCount)
            .SelectMany(row => Enumerable.Range(0, palette.ColorCount)
                .Select(column => palette.GetColor(column, row))).ToArray();

        // Assert
        colors[0].Should().Be(defaultColor);
        colors.Distinct().Should().BeEquivalentTo(expected);
    }
}
