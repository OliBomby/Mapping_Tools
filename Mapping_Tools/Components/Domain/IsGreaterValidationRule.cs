using Mapping_Tools.Classes.SystemTools;
using System.Globalization;
using System.Windows.Controls;

namespace Mapping_Tools.Components.Domain {
    internal class IsGreaterValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo) {
            double limit = ValueWrapper.Value;
            string str = (value ?? "").ToString();
            if (!TypeConverters.TryParseDouble(str, out double result)) {
                return new ValidationResult(false, "Double format error.");
            }
            return result > limit ? ValidationResult.ValidResult : new ValidationResult(false, $"Value must be greater than {limit}.");
        }

        public DoubleWrapper ValueWrapper { get; set; }
    }
}