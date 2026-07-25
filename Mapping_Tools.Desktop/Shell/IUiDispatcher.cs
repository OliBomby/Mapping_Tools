using Avalonia.Threading;

namespace Mapping_Tools.Desktop.Shell;

/// <summary>
/// Marshals application notifications to the desktop UI thread.
/// </summary>
public interface IUiDispatcher
{
    /// <summary>Queues one action on the UI thread.</summary>
    /// <param name="action">The presentation mutation to run.</param>
    void Post(Action action);
}

internal sealed class AvaloniaUiDispatcher : IUiDispatcher
{
    public void Post(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        Dispatcher.UIThread.Post(action);
    }
}
