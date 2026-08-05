using System.ComponentModel.DataAnnotations;

namespace Mapping_Tools.Desktop.ViewModels.Dialogs.Validation;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
internal sealed class DialogValueAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(
        object? value,
        ValidationContext validationContext)
    {
        if (validationContext.ObjectInstance is not ValueDialogViewModel viewModel)
        {
            return new ValidationResult(
                "The dialog value validator is unavailable.",
                validationContext.MemberName is null
                    ? null
                    : [validationContext.MemberName]);
        }

        ValidationResult? result = viewModel.ValidateDialogValue(value);
        return result == ValidationResult.Success
            ? ValidationResult.Success
            : new ValidationResult(
                result?.ErrorMessage ?? "The value is invalid.",
                validationContext.MemberName is null
                    ? null
                    : [validationContext.MemberName]);
    }
}
