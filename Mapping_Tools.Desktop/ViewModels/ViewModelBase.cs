using System.Collections;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using ReactiveUI;

namespace Mapping_Tools.Desktop.ViewModels;

/// <summary>
/// Provides ReactiveUI change notification and annotation-driven validation to
/// Avalonia presentation models.
/// </summary>
public abstract class ViewModelBase : ReactiveObject, INotifyDataErrorInfo
{
    private readonly Dictionary<string, string[]> _validationErrors =
        new(StringComparer.Ordinal);

    /// <inheritdoc/>
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    /// <inheritdoc/>
    public bool HasErrors => _validationErrors.Count > 0;

    /// <inheritdoc/>
    public IEnumerable GetErrors(string? propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
        {
            return _validationErrors.Values.SelectMany(errors => errors).ToArray();
        }

        return _validationErrors.TryGetValue(propertyName, out string[]? errors)
            ? errors
            : Array.Empty<string>();
    }

    /// <summary>
    /// Runs every DataAnnotations rule attached to one changed property and
    /// publishes a stable error snapshot for its binding.
    /// </summary>
    /// <param name="value">The property's current typed value.</param>
    /// <param name="propertyName">The annotated property to validate.</param>
    /// <returns><see langword="true"/> when every property annotation accepts the value.</returns>
    protected bool ValidateProperty(
        object? value,
        [CallerMemberName] string propertyName = "")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

        List<ValidationResult> results = [];
        ValidationContext context = new(this)
        {
            MemberName = propertyName
        };
        bool isValid = Validator.TryValidateProperty(value, context, results);
        ReplaceErrors(
            propertyName,
            results
                .Select(result => result.ErrorMessage)
                .Where(message => !string.IsNullOrWhiteSpace(message))
                .Cast<string>()
                .Distinct(StringComparer.Ordinal)
                .ToArray());
        return isValid;
    }

    /// <summary>
    /// Runs all DataAnnotations rules before a form-level submit operation.
    /// </summary>
    /// <returns><see langword="true"/> when the complete presentation model is valid.</returns>
    protected bool ValidateAllProperties()
    {
        List<ValidationResult> results = [];
        bool isValid = Validator.TryValidateObject(
            this,
            new ValidationContext(this),
            results,
            validateAllProperties: true);

        Dictionary<string, string[]> nextErrors = results
            .SelectMany(result =>
            {
                string[] members = result.MemberNames.DefaultIfEmpty(string.Empty).ToArray();
                return members.Select(member => new
                {
                    Member = member,
                    result.ErrorMessage
                });
            })
            .Where(item =>
                !string.IsNullOrWhiteSpace(item.Member)
                && !string.IsNullOrWhiteSpace(item.ErrorMessage))
            .GroupBy(item => item.Member, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(item => item.ErrorMessage!)
                    .Distinct(StringComparer.Ordinal)
                    .ToArray(),
                StringComparer.Ordinal);

        foreach (string propertyName in _validationErrors.Keys
                     .Union(nextErrors.Keys, StringComparer.Ordinal)
                     .ToArray())
        {
            ReplaceErrors(
                propertyName,
                nextErrors.GetValueOrDefault(propertyName) ?? []);
        }

        return isValid;
    }

    private void ReplaceErrors(string propertyName, string[] errors)
    {
        string[] previous = _validationErrors.GetValueOrDefault(propertyName) ?? [];
        if (previous.SequenceEqual(errors, StringComparer.Ordinal))
        {
            return;
        }

        bool hadErrors = HasErrors;
        if (errors.Length == 0)
        {
            _validationErrors.Remove(propertyName);
        }
        else
        {
            _validationErrors[propertyName] = errors;
        }

        if (hadErrors != HasErrors)
        {
            this.RaisePropertyChanged(nameof(HasErrors));
        }

        ErrorsChanged?.Invoke(
            this,
            new DataErrorsChangedEventArgs(propertyName));
    }
}
