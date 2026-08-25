using System.ComponentModel.DataAnnotations;
using Avalonia.Threading;
using Mapping_Tools.Application.Interactions.Dialogs;
using Mapping_Tools.Desktop.ViewModels.Dialogs;
using Mapping_Tools.Desktop.Views.Dialogs;

namespace Mapping_Tools.Desktop.Platform;

/// <summary>
///     Presents application dialog contracts in the shell's Material-styled DialogHost.
/// </summary>
public sealed class AvaloniaDialogService : IDialogService
{
    /// <summary>
    ///     Creates a service that uses the shell's root DialogHost.
    /// </summary>
    public AvaloniaDialogService()
    {
    }

    /// <inheritdoc />
    public Task<TResult> ShowMessageAsync<TResult>(
        MessageDialogRequest<TResult> request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        return InvokeOnUiThreadAsync(() => ShowMessageOnUiThreadAsync(request, cancellationToken));
    }

    /// <inheritdoc />
    public Task<ValueDialogResult<TValue>> ShowValueAsync<TValue>(
        ValueDialogRequest<TValue> request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        return InvokeOnUiThreadAsync(() => ShowValueOnUiThreadAsync(request, cancellationToken));
    }

    private async Task<TResult> ShowMessageOnUiThreadAsync<TResult>(
        MessageDialogRequest<TResult> request,
        CancellationToken cancellationToken)
    {
        MessageDialog dialog = new();
        var choices = request.Choices
            .Select(choice => new DialogChoiceViewModel(
                choice.Label,
                choice.IsDefault,
                choice.IsCancel,
                () => DialogHostInteraction.Close(
                    DialogHostInteraction.RootIdentifier,
                    new ResultBox<TResult>(choice.Result))))
            .ToList();
        dialog.DataContext = new MessageDialogViewModel(
            request.Title,
            request.Message,
            request.Details,
            choices);

        object? result = await DialogHostInteraction.ShowAsync(
            dialog,
            DialogHostInteraction.RootIdentifier,
            cancellationToken);
        return result is ResultBox<TResult> box
            ? box.Value
            : request.DismissResult;
    }

    private async Task<ValueDialogResult<TValue>> ShowValueOnUiThreadAsync<TValue>(
        ValueDialogRequest<TValue> request,
        CancellationToken cancellationToken)
    {
        ValueDialog dialog = new();
        ValueDialogViewModel viewModel = new(
            request.Title,
            request.Prompt,
            request.InitialValue,
            request.Converter,
            typeof(TValue),
            request.AcceptLabel,
            request.CancelLabel,
            value => Validate(value, request),
            value => DialogHostInteraction.Close(
                DialogHostInteraction.RootIdentifier,
                new ResultBox<TValue>((TValue)value!)),
            () => DialogHostInteraction.Close(DialogHostInteraction.RootIdentifier));
        dialog.DataContext = viewModel;

        object? result = await DialogHostInteraction.ShowAsync(
            dialog,
            DialogHostInteraction.RootIdentifier,
            cancellationToken);
        return result is ResultBox<TValue> box
            ? new ValueDialogResult<TValue>(true, box.Value)
            : new ValueDialogResult<TValue>(false, default);
    }

    private static ValidationResult? Validate<TValue>(
        object? value,
        ValueDialogRequest<TValue> request)
    {
        TValue typedValue;
        try
        {
            typedValue = (TValue)value!;
        }
        catch (InvalidCastException)
        {
            return new ValidationResult(
                "The converted value has an unexpected type.");
        }

        ValidationContext context = new(request)
        {
            MemberName = nameof(request.InitialValue),
            DisplayName = request.Prompt,
        };
        foreach (var validator in request.Validators)
        {
            var result =
                validator.GetValidationResult(typedValue, context);
            if (result != ValidationResult.Success) return result;
        }

        return ValidationResult.Success;
    }

    private static async Task<TResult> InvokeOnUiThreadAsync<TResult>(
        Func<Task<TResult>> action)
    {
        if (Dispatcher.UIThread.CheckAccess()) return await action();

        return await Dispatcher.UIThread.InvokeAsync(action);
    }

    private sealed record ResultBox<T>(T Value);
}
