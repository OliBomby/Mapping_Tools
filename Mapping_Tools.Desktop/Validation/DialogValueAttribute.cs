using Mapping_Tools.Desktop.Localization;
using System.ComponentModel.DataAnnotations;
using Mapping_Tools.Desktop.ViewModels.Dialogs;

namespace Mapping_Tools.Desktop.Validation;

[AttributeUsage(AttributeTargets.Property)]
internal sealed class DialogValueAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(
        object? value,
        ValidationContext validationContext)
    {
        if (validationContext.ObjectInstance is not ValueDialogViewModel viewModel)
            return new ValidationResult(
                "The dialog value validator is unavailable.",
                validationContext.MemberName is null
                    ? null
                    : [validationContext.MemberName]);

        var result = viewModel.ValidateDialogText(value);
        return result == ValidationResult.Success
            ? ValidationResult.Success
            : new ValidationResult(
                result?.ErrorMessage ?? DesktopStrings.Shell_TheValueIsInvalid,
                validationContext.MemberName is null
                    ? null
                    : [validationContext.MemberName]);
    }
}
