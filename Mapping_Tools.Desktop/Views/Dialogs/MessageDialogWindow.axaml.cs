using Avalonia.Controls;

namespace Mapping_Tools.Desktop.Views.Dialogs;

/// <summary>
/// Renders a reusable owner-modal message with typed actions supplied by its view model.
/// </summary>
public partial class MessageDialogWindow : Window
{
    /// <summary>
    /// Loads the compiled message-dialog view.
    /// </summary>
    public MessageDialogWindow()
    {
        InitializeComponent();
    }
}
