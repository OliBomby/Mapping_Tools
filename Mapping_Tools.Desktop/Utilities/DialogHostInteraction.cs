using Avalonia.Threading;
using DialogHostAvalonia;

namespace Mapping_Tools.Desktop.Utilities;

/// <summary>Provides the shared root and nested DialogHost interaction boundary.</summary>
internal static class DialogHostInteraction
{
    /// <summary>Identifies the DialogHost covering the main shell.</summary>
    internal const string RootIdentifier = "RootDialog";

    /// <summary>Identifies DialogHost instances embedded in graph editors.</summary>
    internal const string GraphIdentifier = "GraphDialog";

    /// <summary>Shows content in a named host and closes it when cancellation is requested.</summary>
    /// <param name="content">The control or view model displayed by the host.</param>
    /// <param name="identifier">The host identifier, or <see langword="null" /> for the default host.</param>
    /// <param name="cancellationToken">Cancels the interaction and closes the open host session.</param>
    /// <returns>The value supplied when the host session closes.</returns>
    internal static async Task<object?> ShowAsync(
        object content,
        string identifier,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        cancellationToken.ThrowIfCancellationRequested();

        if (Dispatcher.UIThread.CheckAccess())
        {
            return await ShowOnUiThreadAsync(content, identifier, cancellationToken);
        }

        return await Dispatcher.UIThread.InvokeAsync(
            () => ShowOnUiThreadAsync(content, identifier, cancellationToken));
    }

    /// <summary>Closes a named host session with an optional result.</summary>
    /// <param name="identifier">The host identifier.</param>
    /// <param name="result">The result returned by the pending <see cref="ShowAsync" /> call.</param>
    internal static void Close(string identifier, object? result = null)
    {
        DialogHost.GetDialogSession(identifier)?.Close(result);
    }

    private static async Task<object?> ShowOnUiThreadAsync(
        object content,
        string identifier,
        CancellationToken cancellationToken)
    {
        CancellationTokenRegistration registration = default;
        try
        {
            DialogOpenedEventHandler openedHandler = (_, eventArgs) =>
            {
                registration = cancellationToken.Register(() =>
                    Dispatcher.UIThread.Post(() =>
                    {
                        if (!eventArgs.Session.IsEnded) eventArgs.Session.Close();
                    }));
            };

            object? result = await DialogHost.Show(
                    content,
                    identifier,
                    openedHandler)
                .ConfigureAwait(true);
            cancellationToken.ThrowIfCancellationRequested();
            return result;
        }
        finally
        {
            registration.Dispose();
        }
    }
}
