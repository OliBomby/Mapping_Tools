using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Mapping_Tools.Classes.SystemTools;

namespace Mapping_Tools.Components.Domain {
    internal class IsLessOrEqualValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo) {
            double limit = ValueWrapper.Value;
            string str = (value ?? "").ToString();
            if (!TypeConverters.TryParseDouble(str, out double result)) {
                return new ValidationResult(false, "Double format error.");
            }
            return result <= limit ? ValidationResult.ValidResult : new ValidationResult(false, $"Value can not be greater than {limit}.");
        }

        public DoubleWrapper ValueWrapper { get; set; }
    }

    public class DoubleWrapper : DependencyObject
    {
        public static readonly DependencyProperty FormatProperty =
            DependencyProperty.Register("Value", typeof(double),
                typeof(DoubleWrapper), new FrameworkPropertyMetadata(null));

        public double Value {
            get => (double)GetValue(FormatProperty);
            set => SetValue(FormatProperty, value);
        }
    }
}