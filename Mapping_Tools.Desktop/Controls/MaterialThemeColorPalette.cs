using Avalonia.Controls;
using Avalonia.Media;
using Material.Colors;

namespace Mapping_Tools.Desktop.Controls;

/// <summary>Offers Material.Avalonia's theme colors with the original default first.</summary>
public sealed class MaterialThemeColorPalette : IColorPalette
{
    private readonly Color[] colors;

    private MaterialThemeColorPalette(IEnumerable<MaterialColor> choices, MaterialColor defaultColor)
    {
        colors = choices.Prepend(defaultColor).Distinct()
            .Select(choice => SwatchHelper.Lookup[choice]).ToArray();
        ColorCount = (colors.Length + 3) / 4;
    }

    /// <summary>Gets the primary theme choices, beginning with the original Blue default.</summary>
    public static MaterialThemeColorPalette Primary { get; } = new(
        Enum.GetValues<PrimaryColor>().Select(choice => (MaterialColor)choice),
        (MaterialColor)PrimaryColor.Blue);

    /// <summary>Gets the accent theme choices, beginning with the original Lime default.</summary>
    public static MaterialThemeColorPalette Accent { get; } = new(
        Enum.GetValues<SecondaryColor>().Select(choice => (MaterialColor)choice),
        (MaterialColor)SecondaryColor.Lime);

    /// <inheritdoc />
    public int ColorCount { get; }

    /// <inheritdoc />
    public int ShadeCount => 4;

    /// <inheritdoc />
    public Color GetColor(int colorIndex, int shadeIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(colorIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(colorIndex, ColorCount);
        ArgumentOutOfRangeException.ThrowIfNegative(shadeIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(shadeIndex, ShadeCount);

        int index = shadeIndex * ColorCount + colorIndex;
        // The palette contract requires a rectangle; fill its spare cell with the default.
        return index < colors.Length ? colors[index] : colors[0];
    }
}
