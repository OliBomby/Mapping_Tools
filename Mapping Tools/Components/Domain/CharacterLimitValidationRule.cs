using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace Mapping_Tools.Components.Domain {
    internal class CharacterLimitValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo) {
            int limit = LimitWrapper.Value;
            string str = (value ?? "").ToString();
            return str.Length <= limit ? ValidationResult.ValidResult : new ValidationResult(false, $"Field can not be over {limit} characters long.");
        }

        public IntWrapper LimitWrapper { get; set; }
    }

    public class IntWrapper : DependencyObject
    {
        public static readonly DependencyProperty FormatProperty =
            DependencyProperty.Register("Value", typeof(int),
                typeof(IntWrapper), new FrameworkPropertyMetadata(null));

        public int Value {
            get => (int)GetValue(FormatProperty);
            set => SetValue(FormatProperty, value);
        }
    }

    public class BindingProxy : Freezable
    {
        protected override Freezable CreateInstanceCore() {
            return new BindingProxy();
        }

        public object Data {
            get => GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register("Data", typeof(object), typeof(BindingProxy), new PropertyMetadata(null));
    }
}
