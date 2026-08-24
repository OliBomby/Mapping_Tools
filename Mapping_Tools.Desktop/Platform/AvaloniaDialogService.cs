using System.ComponentModel.DataAnnotations;
using Avalonia.Controls;
using Avalonia.Threading;
using Mapping_Tools.Application.Interactions;
using Mapping_Tools.Application.Interactions.Dialogs;
using Mapping_Tools.Desktop.ViewModels.Dialogs;
using Mapping_Tools.Desktop.Views.Dialogs;

namespace Mapping_Tools.Desktop.Platform;

/// <summary>
///     Presents application dialog contracts as Avalonia 12.1 owner-modal windows.
/// </summary>
public sealed class AvaloniaDialogService : IDialogService
{
    private readonly Func<Window> owner;

    /// <summary>
    ///     Creates a service whose dialogs are always owned by the current shell window.
    /// </summary>
    /// <param name="owner">Returns the initialized window disabled during each modal interaction.</param>
    public AvaloniaDialogService(Func<Window> owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        this.owner = owner;
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
        MessageDialogWindow window = new();
        var choices = request.Choices
            .Select(choice => new DialogChoiceViewModel(
                choice.Label,
                choice.IsDefault,
                choice.IsCancel,
                () => window.Close(new ResultBox<TResult>(choice.Result))))
            .ToList();
        window.DataContext = new MessageDialogViewModel(
            request.Title,
            request.Message,
            request.Details,
            choices);

        var lifetime = window.ShowDialog<object?>(owner());
        using var registration =
            RegisterCancellation(window, cancellationToken);
        object? result = await lifetime;
        cancellationToken.ThrowIfCancellationRequested();
        return result is ResultBox<TResult> box
            ? box.Value
            : request.DismissResult;
    }

    private async Task<ValueDialogResult<TValue>> ShowValueOnUiThreadAsync<TValue>(
        ValueDialogRequest<TValue> request,
        CancellationToken cancellationToken)
    {
        ValueDialogWindow window = new();
        ValueDialogViewModel viewModel = new(
            request.Title,
            request.Prompt,
            request.InitialValue,
            request.Converter,
            typeof(TValue),
            request.AcceptLabel,
            request.CancelLabel,
            value => Validate(value, request),
            value => window.Close(new ResultBox<TValue>((TValue)value!)),
            () => window.Close());
        window.DataContext = viewModel;

        var lifetime = window.ShowDialog<object?>(owner());
        using var registration =
            RegisterCancellation(window, cancellationToken);
        object? result = await lifetime;
        cancellationToken.ThrowIfCancellationRequested();
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

    private static CancellationTokenRegistration RegisterCancellation(
        Window window,
        CancellationToken cancellationToken)
    {
        return cancellationToken.Register(() =>
            window.Dispatcher.Post(() =>
            {
                if (window.IsVisible) window.Close();
            }));
    }

    private static async Task<TResult> InvokeOnUiThreadAsync<TResult>(
        Func<Task<TResult>> action)
    {
        if (Dispatcher.UIThread.CheckAccess()) return await action();

        return await Dispatcher.UIThread.InvokeAsync(action);
    }

    private sealed record ResultBox<T>(T Value);
}
