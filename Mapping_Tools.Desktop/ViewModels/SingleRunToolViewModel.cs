using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.ToolExecution.Models;
using Mapping_Tools.Application.Tools;

namespace Mapping_Tools.Desktop.ViewModels;

/// <summary>
///     Provides the shared command and presentation state for a tool that has one
///     ordinary run at a time.
/// </summary>
public abstract class SingleRunToolViewModel : ObservableValidator
{
    private long runGeneration;

    /// <summary>
    ///     Creates a single-run tool presentation model.
    /// </summary>
    /// <param name="execution">Coordinates cancellation for the tool operation.</param>
    /// <param name="tool">The canonical application definition for this tool.</param>
    /// <exception cref="ArgumentNullException"><paramref name="execution" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="tool" /> is <see langword="null" />.</exception>
    protected SingleRunToolViewModel(
        IToolExecutionService execution,
        ToolDefinition tool)
    {
        Execution = execution ?? throw new ArgumentNullException(nameof(execution));
        Tool = tool ?? throw new ArgumentNullException(nameof(tool));

        RunCommand = new AsyncRelayCommand(RunAsync, CanRun);
    }

    /// <summary>Gets the canonical application metadata for this tool.</summary>
    protected ToolDefinition Tool { get; }

    /// <summary>Gets the execution service shared by this tool's operations.</summary>
    protected IToolExecutionService Execution { get; }

    /// <summary>Gets whether the ordinary tool run is currently active.</summary>
    public bool IsRunning
    {
        get;
        private set
        {
            if (SetProperty(ref field, value)) RunCommand.NotifyCanExecuteChanged();
        }
    }

    /// <summary>Gets the current ordinary-run completion as a percentage for the legacy progress-bar binding.</summary>
    public double Progress
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>Gets the command that starts the tool's ordinary run.</summary>
    public IAsyncRelayCommand RunCommand { get; }

    /// <summary>
    ///     Validates data-annotation attributes before the run state is entered.
    ///     Derived view models may add presentation-specific preflight behavior.
    /// </summary>
    /// <returns><see langword="true" /> when the ordinary run may start.</returns>
    protected virtual bool PrepareRun()
    {
        ValidateAllProperties();
        return !HasErrors;
    }

    /// <summary>Executes the feature-specific ordinary run.</summary>
    /// <returns>A task that completes when the ordinary run reaches a terminal state.</returns>
    protected abstract Task RunCoreAsync();

    /// <summary>
    ///     Runs an operation while maintaining the shared busy and progress state.
    /// </summary>
    /// <param name="operation">The feature-specific operation to invoke.</param>
    /// <returns>A task that completes when the operation reaches a terminal state.</returns>
    protected async Task RunWithStateAsync(Func<Task> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        if (IsRunning) return;

        IsRunning = true;
        Progress = 0;
        Interlocked.Increment(ref runGeneration);
        try
        {
            await operation();
        }
        finally
        {
            Interlocked.Increment(ref runGeneration);
            Progress = 0;
            IsRunning = false;
        }
    }

    /// <summary>Creates a progress receiver that updates the shared progress property.</summary>
    /// <returns>A progress receiver for the tool execution service.</returns>
    protected IProgress<ToolExecutionProgress> CreateProgress()
    {
        long runGeneration2 = Volatile.Read(ref runGeneration);
        return new Progress<ToolExecutionProgress>(value =>
        {
            if (IsRunning && Volatile.Read(ref runGeneration) == runGeneration2) Progress = value.Progress * 100;
        });
    }

    private bool CanRun()
    {
        return !IsRunning;
    }

    private async Task RunAsync()
    {
        ClearFocusedElement();
        if (PrepareRun()) await RunWithStateAsync(RunCoreAsync);
    }

    private static void ClearFocusedElement()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow?.FocusManager.Focus(null);
    }
}
