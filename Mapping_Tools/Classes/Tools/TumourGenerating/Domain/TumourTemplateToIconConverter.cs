using System;
using System.Globalization;
using System.Windows.Data;
using Mapping_Tools.Classes.Tools.TumourGenerating.Enums;
using MaterialDesignThemes.Wpf;

namespace Mapping_Tools.Classes.Tools.TumourGenerating.Domain {
    public class TumourTemplateToIconConverter : IValueConverter {
        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return value switch {
                TumourTemplate.Triangle => PackIconKind.TriangleOutline,
                TumourTemplate.Square => PackIconKind.SquareOutline,
                TumourTemplate.Circle => PackIconKind.CircleOutline,
                TumourTemplate.Parabola => PackIconKind.Multiply,
                _ => PackIconKind.TriangleOutline,
            };
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return value switch {
                PackIconKind.TriangleOutline => TumourTemplate.Triangle,
                PackIconKind.SquareOutline => TumourTemplate.Square,
                PackIconKind.CircleOutline => TumourTemplate.Circle,
                PackIconKind.Multiply => TumourTemplate.Parabola,
                _ => TumourTemplate.Triangle,
            };
        }
    }
}