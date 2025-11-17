using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Material.Icons;

namespace Mapping_Tools.Desktop.Helpers;

public class NotificationBellIconKindConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isOpen = values.Count > 0 && values[0] is bool b1 && b1;
        bool hasUnread = values.Count > 1 && values[1] is bool b2 && b2;

        if (isOpen) return MaterialIconKind.Bell;
        return hasUnread ? MaterialIconKind.BellBadgeOutline : MaterialIconKind.BellOutline;
    }
}

