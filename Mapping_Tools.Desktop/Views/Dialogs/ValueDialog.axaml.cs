using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace Mapping_Tools.Desktop.Views.Dialogs;

/// <summary>
///     Renders one typed, validated field and keeps focus behavior in the visual layer.
/// </summary>
public partial class ValueDialog : UserControl
{
    /// <summary>
    ///     Loads the compiled value-dialog view.
    /// </summary>
    public ValueDialog()
    {
        InitializeComponent();
    }

    /// <inheritdoc />
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        Dispatcher.UIThread.Post(
            () =>
            {
                ValueTextBox.Focus();
                ValueTextBox.SelectAll();
            },
            DispatcherPriority.Input);
    }
}
