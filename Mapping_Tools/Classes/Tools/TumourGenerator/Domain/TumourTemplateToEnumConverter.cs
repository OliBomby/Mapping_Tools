using System;
using System.Globalization;
using System.Windows.Data;
using Mapping_Tools.Classes.Tools.TumourGenerator.Enums;
using Mapping_Tools.Classes.Tools.TumourGenerator.Options.TumourTemplates;

namespace Mapping_Tools.Classes.Tools.TumourGenerator.Domain
{
    public class TumourTemplateToEnumConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return value switch {
                TriangleTemplate => TumourTemplate.Triangle,
                _ => TumourTemplate.Custom
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
