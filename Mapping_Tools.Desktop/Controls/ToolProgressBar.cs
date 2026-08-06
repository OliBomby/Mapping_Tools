using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace Mapping_Tools.Desktop.Controls;

/// <summary>
/// Shows non-zero tool progress and clears a completed indicator after one second.
/// </summary>
public sealed class ToolProgressBar : ProgressBar
{
    private int _changeVersion;

    static ToolProgressBar()
    {
        ValueProperty.Changed.AddClassHandler<ToolProgressBar>(
            static (progressBar, _) => progressBar.UpdateVisibility());
    }

    /// <summary>Creates an initially hidden progress indicator.</summary>
    public ToolProgressBar() => IsVisible = false;

    /// <inheritdoc/>
    protected override void OnLoaded(RoutedEventArgs eventArgs)
    {
        base.OnLoaded(eventArgs);
        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        int version = ++_changeVersion;
        IsVisible = Value > Minimum;
        if (Value < Maximum)
        {
            return;
        }

        _ = HideCompletedAsync(version);
    }

    private async Task HideCompletedAsync(int version)
    {
        await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
        Dispatcher.UIThread.Post(() =>
        {
            if (version != _changeVersion || Value < Maximum)
            {
                return;
            }

            IsVisible = false;
        });
    }
}
