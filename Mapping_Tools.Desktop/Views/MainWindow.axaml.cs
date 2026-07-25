using Avalonia.Controls;

namespace Mapping_Tools.Desktop.Views;

/// <summary>
/// Hosts the current Avalonia migration shell and exposes its top-level
/// platform services to constructor-injected adapters.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Loads the compiled main-window view.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
    }
}
