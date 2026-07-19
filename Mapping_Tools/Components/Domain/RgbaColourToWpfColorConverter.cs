using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Mapping_Tools.Classes.BeatmapHelper;

namespace Mapping_Tools.Components.Domain;

public sealed class RgbaColourToWpfColorConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
        return value is RgbaColour colour
            ? Color.FromArgb(colour.A, colour.R, colour.G, colour.B)
            : Binding.DoNothing;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
        return value is Color colour
            ? RgbaColour.FromArgb(colour.A, colour.R, colour.G, colour.B)
            : Binding.DoNothing;
    }
}
