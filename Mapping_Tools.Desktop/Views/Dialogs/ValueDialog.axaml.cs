using Avalonia.Controls;

namespace Mapping_Tools.Desktop.Views.Dialogs;

/// <summary>
///     Renders one typed, validated field and keeps focus behavior in the visual layer.
/// </summary>
public partial class ValueDialog : UserControl
{
    /// <summary>
    ///     Loads the compiled value-dialog view and selects the initial text when attached.
    /// </summary>
    public ValueDialog()
    {
        InitializeComponent();
        AttachedToVisualTree += (_, _) =>
        {
            ValueTextBox.Focus();
            ValueTextBox.SelectAll();
        };
    }
}
