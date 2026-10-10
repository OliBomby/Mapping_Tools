using Avalonia.Controls;
using Mapping_Tools.Desktop.Services.Undo;

namespace Mapping_Tools.Desktop.Tools.GeometryDashboard.Views;

/// <summary>Hosts the Geometry Dashboard preferences dialog.</summary>
public sealed partial class GeometryDashboardPreferencesWindow : Window
{
    /// <summary>Creates the preferences window.</summary>
    public GeometryDashboardPreferencesWindow()
    {
        InitializeComponent();
        ProjectUndoWindowInput.AttachDialog(this);
    }
}
