using Avalonia.Controls;
using Avalonia.Media;
using Mapping_Tools.Core.Graph;
using Mapping_Tools.Core.Graph.Markers;
using Mapping_Tools.Desktop.Controls;
using Mapping_Tools.Desktop.ViewModels.Dialogs;

namespace Mapping_Tools.Desktop.Views.Dialogs;

/// <summary>Hosts the reusable graph control in a modal, clone-on-accept editor.</summary>
public partial class GraphEditorWindow : Window
{
    /// <summary>Creates the default graph editor instance required by compiled AXAML.</summary>
    public GraphEditorWindow() : this(new GraphEditorViewModel(GraphState.CreateDefault()))
    {
    }

    /// <summary>Creates a graph editor for the supplied independent view-model snapshot.</summary>
    /// <param name="viewModel">The graph editor state and commands.</param>
    public GraphEditorWindow(GraphEditorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        GraphControl.MinMarkerSpacing = 20;
        GraphControl.SetBrush(new SolidColorBrush(Color.FromArgb(255, 0, 255, 255)));
        GraphControl.HorizontalMarkerGenerator = new DoubleMarkerGenerator(0, 0.25);
        GraphControl.VerticalMarkerGenerator = new DoubleMarkerGenerator(0, 0.25);
        viewModel.Accepted += OnAccepted;
        viewModel.Canceled += OnCanceled;
        Opened += (_, _) => GraphControl.Focus();
        Closed += (_, _) =>
        {
            viewModel.Accepted -= OnAccepted;
            viewModel.Canceled -= OnCanceled;
        };
    }

    /// <summary>Gets the graph control hosted by this dialog.</summary>
    public GraphControl GraphControl => GraphControlElement;

    private void OnAccepted(object? sender, EventArgs eventArgs)
    {
        Close(true);
    }

    private void OnCanceled(object? sender, EventArgs eventArgs)
    {
        Close(false);
    }
}
