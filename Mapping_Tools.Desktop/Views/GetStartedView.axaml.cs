using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mapping_Tools.Desktop.ViewModels;

namespace Mapping_Tools.Desktop.Views;

/// <summary>
/// Renders the offline Get started landing page inside the main shell.
/// </summary>
public partial class GetStartedView : UserControl
{
    /// <summary>Loads the compiled landing-page view.</summary>
    public GetStartedView()
    {
        InitializeComponent();
        Loaded += (_, _) =>
            Dispatcher.UIThread.Post(SizeRecentPathColumn, DispatcherPriority.Loaded);
        DataContextChanged += (_, _) => SizeRecentPathColumn();
    }

    private void SizeRecentPathColumn()
    {
        if (DataContext is not GetStartedViewModel viewModel)
        {
            return;
        }

        double width = 90;
        foreach (RecentMapViewModel recentMap in viewModel.RecentMaps)
        {
            TextBlock measurement = new()
            {
                Text = recentMap.FileName,
                FontFamily = FontFamily,
                FontSize = FontSize,
                FontWeight = FontWeight
            };
            measurement.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            width = Math.Max(width, Math.Ceiling(measurement.DesiredSize.Width + 16));
        }

        RecentPathColumn.Width = new GridLength(width);
        RecentMapsTable.InvalidateMeasure();

        foreach (Control descendant in RecentMapsTable.GetVisualDescendants().OfType<Control>())
        {
            descendant.InvalidateMeasure();
        }
    }
}
