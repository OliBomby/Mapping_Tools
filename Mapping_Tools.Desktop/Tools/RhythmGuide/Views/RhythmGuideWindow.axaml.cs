using Avalonia.Controls;
using Mapping_Tools.Desktop.Services.Undo;
using Mapping_Tools.Desktop.Tools.RhythmGuide.ViewModels;

namespace Mapping_Tools.Desktop.Tools.RhythmGuide.Views;

/// <summary>Hosts the shared Rhythm Guide view model in a modeless auxiliary window.</summary>
public sealed partial class RhythmGuideWindow : Window
{
    /// <summary>Creates the auxiliary Rhythm Guide window.</summary>
    public RhythmGuideWindow()
    {
        InitializeComponent();
        ProjectUndoWindowInput.Attach(this,
            () => (DataContext as RhythmGuideViewModel)?.UndoHistory);
    }
}
