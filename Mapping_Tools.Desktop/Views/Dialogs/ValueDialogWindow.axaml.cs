using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Mapping_Tools.Desktop.ViewModels.Dialogs;

namespace Mapping_Tools.Desktop.Views.Dialogs;

/// <summary>
/// Renders one typed, validated field and keeps focus behavior in the visual layer.
/// </summary>
public partial class ValueDialogWindow : Window
{
    /// <summary>
    /// Loads the compiled value-dialog view and selects the initial text when opened.
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

    /// <summary>
    /// Connects the field to its typed value through the request-specific converter.
    /// </summary>
    /// <param name="converter">
    /// The converter that formats the initial typed value and parses subsequent edits.
    /// </param>
    public void BindValue(IValueConverter converter)
    {
        ArgumentNullException.ThrowIfNull(converter);
        ValueTextBox.Bind(
            TextBox.TextProperty,
            new Binding(nameof(ValueDialogViewModel.Value))
            {
                Mode = BindingMode.TwoWay,
                Converter = converter
            });
    }
}
