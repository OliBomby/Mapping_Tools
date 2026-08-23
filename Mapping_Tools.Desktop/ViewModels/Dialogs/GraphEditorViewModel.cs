using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Core.Graph;

namespace Mapping_Tools.Desktop.ViewModels.Dialogs;

/// <summary>Owns the cloned graph state and modal actions for the graph editor window.</summary>
public sealed class GraphEditorViewModel : ObservableObject
{
    private GraphState graphState;

    /// <summary>Creates graph-editor state from an independent edit snapshot.</summary>
    /// <param name="graphState">The graph state to edit.</param>
    public GraphEditorViewModel(GraphState graphState)
    {
        this.graphState = graphState?.Clone() ?? throw new ArgumentNullException(nameof(graphState));
        AcceptCommand = new RelayCommand(Accept);
        CancelCommand = new RelayCommand(Cancel);
    }

    /// <summary>Gets or sets the graph snapshot edited by the window.</summary>
    public GraphState GraphState
    {
        get => graphState;
        set => SetProperty(ref graphState, value ?? throw new ArgumentNullException(nameof(value)));
    }

    /// <summary>Gets the command that accepts graph edits.</summary>
    public IRelayCommand AcceptCommand { get; }

    /// <summary>Gets the command that discards graph edits.</summary>
    public IRelayCommand CancelCommand { get; }

    /// <summary>Raised when the user accepts the current graph.</summary>
    public event EventHandler? Accepted;

    /// <summary>Raised when the user cancels graph editing.</summary>
    public event EventHandler? Canceled;

    private void Accept()
    {
        Accepted?.Invoke(this, EventArgs.Empty);
    }

    private void Cancel()
    {
        Canceled?.Invoke(this, EventArgs.Empty);
    }
}
