using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Mapping_Tools.Desktop.Helpers;

public class TimeAgoConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DateTime dt)
            return string.Empty;

        var now = DateTime.Now;
        var ts = now - dt;
        if (ts.TotalSeconds < 5) return "just now";
        if (ts.TotalSeconds < 60) return $"{Math.Floor(ts.TotalSeconds)}s ago";
        if (ts.TotalMinutes < 60) return $"{Math.Floor(ts.TotalMinutes)}m ago";
        if (ts.TotalHours < 24) return $"{Math.Floor(ts.TotalHours)}h ago";
        if (ts.TotalDays < 7) return $"{Math.Floor(ts.TotalDays)}d ago";
        if (ts.TotalDays < 30) return $"{Math.Floor(ts.TotalDays / 7)}w ago";
        if (ts.TotalDays < 365) return $"{Math.Floor(ts.TotalDays / 30)}mo ago";
        return $"{Math.Floor(ts.TotalDays / 365)}y ago";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new InvalidOperationException("TimeAgoConverter cannot convert back.");
}

