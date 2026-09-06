using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace Mapping_Tools.Desktop.Tools.PatternGallery.Views;

/// <summary>Dialog for renaming a Pattern Gallery collection and its directory.</summary>
public sealed partial class PatternGalleryCollectionRenameDialog : UserControl
{
    /// <summary>Creates the collection rename form.</summary>
    public PatternGalleryCollectionRenameDialog()
    {
        InitializeComponent();
    }

    /// <inheritdoc />
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        Dispatcher.UIThread.Post(
            ()
                =>
            {
                NewNameTextBox.Focus();
                NewNameTextBox.SelectAll();
            },
            DispatcherPriority.Input);
    }
}
