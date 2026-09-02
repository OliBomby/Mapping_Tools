using System.ComponentModel.DataAnnotations;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Mapping_Tools.Desktop.Utilities;
using Mapping_Tools.Desktop.ViewModels.Dialogs;
using Mapping_Tools.Desktop.Views.Dialogs;

namespace Mapping_Tools.Desktop.Services.Dialogs;

/// <summary>
///     Presents application dialog contracts as owner-modal Avalonia windows and
///     shell-hosted Material dialogs.
/// </summary>
public sealed class DialogService : IDialogService
{
    /// <summary>
    ///     Creates a service that presents dialogs through the desktop application lifetime.
    /// </summary>
    public DialogService()
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
                () => dialog.Close(new ResultBox<TResult>(choice.Result))))
            .ToList();
        dialog.DataContext = new MessageDialogViewModel(
            request.Title,
            request.Message,
            request.Details,
            choices);

        Task<object?> dialogTask = dialog.ShowDialog<object?>(GetOwnerWindow());
        using CancellationTokenRegistration registration = cancellationToken.Register(
            () => Dispatcher.UIThread.Post(
                () =>
                {
                    if (dialog.IsVisible) dialog.Close();
                }));

        object? result = await dialogTask;
        cancellationToken.ThrowIfCancellationRequested();
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

    private static Window GetOwnerWindow()
    {
        if (global::Avalonia.Application.Current?.ApplicationLifetime
                is IClassicDesktopStyleApplicationLifetime
                {
                    MainWindow: Window mainWindow,
                }) return mainWindow;

        throw new InvalidOperationException(
            "A desktop main window is required to show an owner-modal message dialog.");
    }

    private sealed record ResultBox<T>(T Value);
}
