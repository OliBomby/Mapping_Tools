using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Mapping_Tools.Desktop.ViewModels.GetStarted;

namespace Mapping_Tools.Desktop.Views.GetStarted;

/// <summary>
///     Renders the offline Get started landing page inside the main shell.
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
        if (DataContext is not GetStartedViewModel viewModel) return;

        double width = 90;
        foreach (var recentMap in viewModel.RecentMaps)
        {
            TextBlock measurement = new()
            {
                Text = recentMap.FileName,
                FontFamily = FontFamily,
                FontSize = FontSize,
                FontWeight = FontWeight,
            };
            measurement.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            width = Math.Max(width, Math.Ceiling(measurement.DesiredSize.Width + 16));
        }

        RecentPathColumn.Width = new GridLength(width);
        RecentMapsTable.InvalidateMeasure();

        foreach (var descendant in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants(RecentMapsTable).OfType<Control>()) descendant.InvalidateMeasure();
    }

    private void SelectRecentMaps(object? sender, TappedEventArgs eventArgs)
    {
        if (DataContext is not GetStartedViewModel viewModel) return;

        viewModel.SelectRecentMaps(
            RecentMapsTable.SelectedItems?.OfType<RecentMapViewModel>() ?? []);
        eventArgs.Handled = true;
    }
}
