using System;
using System.Globalization;
using System.Windows.Data;
using Mapping_Tools.Classes.BeatmapHelper;

namespace Mapping_Tools.Components.Domain {
    public class CircleSizeToThicknessConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return value is double d ? Beatmap.GetHitObjectRadius(d) * 2 : 50;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotSupportedException("CircleSizeToThicknessConverter is a OneWay converter.");
        }
    }
}
