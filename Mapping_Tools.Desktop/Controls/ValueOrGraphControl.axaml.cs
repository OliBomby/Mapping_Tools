using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Mapping_Tools.Core.Classes.Graph;
using Mapping_Tools.Core.Classes.MathUtil;
using Mapping_Tools.Desktop.ViewModels.Dialogs;
using Mapping_Tools.Desktop.Views.Dialogs;
using CoreGraphState = Mapping_Tools.Core.Classes.Graph.GraphState;

namespace Mapping_Tools.Desktop.Controls;

/// <summary>Provides the legacy constant-text or modal-graph editing surface.</summary>
public partial class ValueOrGraphControl : UserControl
{
    /// <summary>Identifies the graph state edited by this value control.</summary>
    public static readonly StyledProperty<CoreGraphState?> GraphStateProperty =
        AvaloniaProperty.Register<ValueOrGraphControl, CoreGraphState?>(
            nameof(GraphState),
            defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Loads the compiled value-or-graph view with an independent default graph.</summary>
    public ValueOrGraphControl()
    {
        InitializeComponent();
        SetCurrentValue(GraphStateProperty, CreateDefaultValueGraphState());
    }

    /// <summary>Gets or sets the scalar or graph state exposed to the host feature.</summary>
    public CoreGraphState? GraphState
    {
        get => GetValue(GraphStateProperty);
        set => SetValue(GraphStateProperty, value);
    }

    private async void OpenGraphEditor(object? sender, RoutedEventArgs eventArgs)
    {
        if (TopLevel.GetTopLevel(this) is not Window owner) return;

        GraphEditorViewModel viewModel = new(GraphState?.Clone() ?? CreateDefaultValueGraphState());
        GraphEditorWindow dialog = new(viewModel);
        bool accepted = await dialog.ShowDialog<bool>(owner);
        if (accepted) SetCurrentValue(GraphStateProperty, viewModel.GraphState.Clone());

        eventArgs.Handled = true;
    }

    private static CoreGraphState CreateDefaultValueGraphState()
    {
        return new CoreGraphState(
            [
                new GraphAnchor(new Vector2(0, 0)),
                new GraphAnchor(new Vector2(1, 1)),
            ],
            0,
            0,
            1,
            1);
    }
}
