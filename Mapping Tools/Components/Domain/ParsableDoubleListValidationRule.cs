using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace Mapping_Tools.Components.Domain
{
    class ParsableDoubleListValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo) {
            if (Regex.IsMatch((value ?? "").ToString(), @"^([0-9]+(\.[0-9]+)?(,[0-9]+(\.[0-9]+)?)*)?$")) {
                return ValidationResult.ValidResult;
            } else {
                return new ValidationResult(false, "Field cannot be parsed.");
            }
        }
    }
}
