using System;
using System.Globalization;
using System.Windows.Data;
using Mapping_Tools.Classes.Tools.TumourGenerating.Enums;
using Mapping_Tools.Classes.Tools.TumourGenerating.Options.TumourTemplates;

namespace Mapping_Tools.Classes.Tools.TumourGenerating.Domain
{
    public class TumourTemplateToEnumConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return value switch {
                TriangleTemplate => TumourTemplate.Triangle,
                SquareTemplate => TumourTemplate.Square,
                CircleTemplate => TumourTemplate.Circle,
                ParabolaTemplate => TumourTemplate.Parabola,
                _ => TumourTemplate.Custom
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return value switch {
                TumourTemplate.Triangle => new TriangleTemplate(),
                TumourTemplate.Square => new SquareTemplate(),
                TumourTemplate.Circle => new CircleTemplate(),
                TumourTemplate.Parabola => new ParabolaTemplate(),
                _ => new TriangleTemplate()
            };
        }
    }
}
