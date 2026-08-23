using Avalonia.Controls;

namespace Mapping_Tools.Desktop.Views.Dialogs;

/// <summary>
///     Renders one typed, validated field and keeps focus behavior in the visual layer.
/// </summary>
public partial class ValueDialogWindow : Window
{
    /// <summary>
    ///     Loads the compiled value-dialog view and selects the initial text when opened.
    /// </summary>
    public ValueDialogWindow()
    {
        InitializeComponent();
        Opened += (_, _) =>
        {
            ValueTextBox.Focus();
            ValueTextBox.SelectAll();
        };
    }
}
