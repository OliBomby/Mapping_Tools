using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace Mapping_Tools.Components.Domain
{
    class IsASCIIValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo) {
            string str = (value ?? "").ToString();
            if (Encoding.UTF8.GetByteCount(str) == str.Length) {
                return ValidationResult.ValidResult;
            } else {
                return new ValidationResult(false, "Field is not ASCII.");
            }
        }
    }
}
