using Avalonia.Controls;

namespace Mapping_Tools.Desktop.Views.Dialogs;

/// <summary>
///     Renders a reusable message dialog with typed actions supplied by its view model.
/// </summary>
public partial class MessageDialog : UserControl
{
    /// <summary>
    ///     Loads the compiled message-dialog view.
    /// </summary>
    public MessageDialog()
    {
        InitializeComponent();
    }
}
