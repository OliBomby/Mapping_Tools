using System.ComponentModel.DataAnnotations;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Mapping_Tools.Desktop.Utilities;
using Mapping_Tools.Desktop.ViewModels.Dialogs;
using Mapping_Tools.Desktop.Views.Dialogs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mapping_Tools.Desktop.Services.Dialogs;

/// <summary>
///     Presents application dialog contracts as owner-modal Avalonia windows and
///     shell-hosted Material dialogs.
/// </summary>
public sealed class DialogService : IDialogService
{
    private readonly ILogger<DialogService> logger;

    /// <summary>
    ///     Creates a service that presents dialogs through the desktop application lifetime.
    /// </summary>
    /// <param name="logger">Records dialog decisions.</param>
    public DialogService(ILogger<DialogService>? logger = null)
    {
        this.logger = logger ?? NullLogger<DialogService>.Instance;
    }

    /// <inheritdoc />
    public Task<TResult> ShowMessageAsync<TResult>(
        MessageDialogRequest<TResult> request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        logger.LogInformation("Message dialog opened: {Title}", request.Title);
        return InvokeOnUiThreadAsync(() => ShowMessageOnUiThreadAsync(request, cancellationToken));
    }

    /// <inheritdoc />
    public Task<ValueDialogResult<TValue>> ShowValueAsync<TValue>(
        ValueDialogRequest<TValue> request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        logger.LogInformation("Value dialog opened: {Title}; prompt {Prompt}", request.Title, request.Prompt);
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
                () =>
                {
                    logger.LogInformation("Message dialog {Title}: user chose {Choice}", request.Title, choice.Label);
                    dialog.Close(new ResultBox<TResult>(choice.Result));
                }))
            .ToList();
        dialog.DataContext = new MessageDialogViewModel(
            request.Title,
            request.Message,
            request.Details,
            choices);

        var dialogTask = dialog.ShowDialog<object?>(GetOwnerWindow());
        await using var registration = cancellationToken.Register(() => Dispatcher.UIThread.Post(() =>
        {
            if (dialog.IsVisible) dialog.Close();
        }));

        object? result = await dialogTask;
        cancellationToken.ThrowIfCancellationRequested();
        if (result is not ResultBox<TResult>) logger.LogInformation("Message dialog {Title} dismissed", request.Title);
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
            value =>
            {
                logger.LogInformation("Value dialog {Title} accepted value {Value}", request.Title, value);
                DialogHostInteraction.Close(
                    DialogHostInteraction.ROOT_IDENTIFIER,
                    new ResultBox<TValue>((TValue)value!));
            },
            () =>
            {
                logger.LogInformation("Value dialog {Title} cancelled", request.Title);
                DialogHostInteraction.Close(DialogHostInteraction.ROOT_IDENTIFIER);
            });
        dialog.DataContext = viewModel;

        object? result = await DialogHostInteraction.ShowAsync(
            dialog,
            DialogHostInteraction.ROOT_IDENTIFIER,
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
        if (Avalonia.Application.Current?.ApplicationLifetime
            is IClassicDesktopStyleApplicationLifetime
            {
                MainWindow: { } mainWindow,
            }) return mainWindow;

        throw new InvalidOperationException(
            "A desktop main window is required to show an owner-modal message dialog.");
    }

    private sealed record ResultBox<T>(T Value);
}
