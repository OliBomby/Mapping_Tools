using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Mapping_Tools.Core.Graph;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Desktop.Platform;
using Mapping_Tools.Desktop.ViewModels.Dialogs;
using Mapping_Tools.Desktop.Views.Dialogs;
using CoreGraphState = Mapping_Tools.Core.Graph.GraphState;

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
        if (TopLevel.GetTopLevel(this) is null) return;

        GraphEditorViewModel viewModel = new(GraphState?.Clone() ?? CreateDefaultValueGraphState());
        GraphEditorDialog dialog = new(viewModel);
        dialog.Close = result => DialogHostInteraction.Close(
            DialogHostInteraction.RootIdentifier,
            result);
        object? result = await DialogHostInteraction.ShowAsync(
            dialog,
            DialogHostInteraction.RootIdentifier);
        if (result is true) SetCurrentValue(GraphStateProperty, viewModel.GraphState.Clone());

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
