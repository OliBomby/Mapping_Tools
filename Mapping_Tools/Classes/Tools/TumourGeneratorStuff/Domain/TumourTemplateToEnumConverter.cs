using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;
using Mapping_Tools.Classes.Tools.TumourGeneratorStuff.Enums;
using Mapping_Tools.Classes.Tools.TumourGeneratorStuff.Options.TumourTemplates;
using Mapping_Tools.Components.Domain;

namespace Mapping_Tools.Classes.Tools.TumourGeneratorStuff.Domain
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
