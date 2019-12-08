using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace Mapping_Tools.Components.Domain
{
    internal class ParsableDoubleListValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo) {
            return Regex.IsMatch((value ?? "").ToString(), @"^([0-9]+(\.[0-9]+)?(,[0-9]+(\.[0-9]+)?)*)?$") ? 
                ValidationResult.ValidResult : 
                new ValidationResult(false, "Field cannot be parsed.");
        }
    }
}
