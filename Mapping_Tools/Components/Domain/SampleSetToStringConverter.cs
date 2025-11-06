using System;
using System.Globalization;
using System.Windows.Data;
using Mapping_Tools.Classes.BeatmapHelper.Enums;

namespace Mapping_Tools.Components.Domain;

class SampleSetToStringConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
        return ((SampleSet)value).ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
        string str = value.ToString();
        return Enum.Parse(typeof(SampleSet), str);
    }
}