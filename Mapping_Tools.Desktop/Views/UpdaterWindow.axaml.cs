using Avalonia.Controls;

namespace Mapping_Tools.Desktop.Views;

/// <summary>
/// Displays the release notes, updater decisions, and package progress.
/// </summary>
public sealed partial class UpdaterWindow : Window
{
    /// <summary>Creates the updater window; its state is supplied by the data context.</summary>
    public UpdaterWindow()
    {
        InitializeComponent();
    }
}
