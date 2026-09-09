using Avalonia.Controls;
using Avalonia.Input;

namespace Mapping_Tools.Desktop.Tools.HitsoundStudio.Views;

/// <summary>Hosts the typed Hitsound Studio layer import form in its own window.</summary>
public sealed partial class HitsoundStudioImportDialog : Window
{
    /// <summary>Creates the import form.</summary>
    public HitsoundStudioImportDialog()
    {
        InitializeComponent();
    }

    private void DragWindow(object? sender, PointerPressedEventArgs eventArgs)
    {
        if (eventArgs.GetCurrentPoint(this).Properties.IsLeftButtonPressed) BeginMoveDrag(eventArgs);
    }
}
