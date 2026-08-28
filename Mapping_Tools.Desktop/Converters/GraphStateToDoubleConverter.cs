using System.Globalization;
using Avalonia.Data.Converters;
using Mapping_Tools.Core.Graph;
using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Desktop.Converters;

/// <summary>Bridges the legacy scalar slider surface to the shared graph state.</summary>
public sealed class GraphStateToDoubleConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return GetScalarValue(value as GraphState);
    }

    internal static double GetScalarValue(GraphState? state)
    {
        if (state is null || state.Anchors.Count == 0) return 0d;

        double firstValue = state.Anchors[0].Pos.Y;
        if (state.Anchors.All(anchor => Precision.AlmostEquals(anchor.Pos.Y, firstValue))) return firstValue;

        double width = state.MaxX - state.MinX;
        return Math.Abs(width) <= Precision.DOUBLE_EPSILON
            ? firstValue
            : state.GetIntegral(state.MinX, state.MaxX) / width;
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double number && double.IsFinite(number)) return GraphStateTextCodec.CreateConstant(number);

        return null!;
    }
}
