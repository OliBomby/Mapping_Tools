using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace Mapping_Tools.Components.Domain;

public class BooleanConverter<T> : IValueConverter, IMultiValueConverter {
    public BooleanConverter(T trueValue, T falseValue) {
        True = trueValue;
        False = falseValue;
    }

    public T True { get; set; }
    public T False { get; set; }

    public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
        return value is bool && ((bool)value) ? True : False;
    }

    public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
        return value is T && EqualityComparer<T>.Default.Equals((T)value, True);
    }

    public virtual object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        return values[0] is bool && ((bool)values[0]) ? True : False;
    }

    public virtual object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}