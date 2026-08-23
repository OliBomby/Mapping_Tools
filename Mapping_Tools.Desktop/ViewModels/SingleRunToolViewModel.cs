using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.Execution;

namespace Mapping_Tools.Desktop.ViewModels;

/// <summary>
///     Provides the shared command and presentation state for a tool that has one
///     ordinary run at a time.
/// </summary>
public abstract class SingleRunToolViewModel : ObservableValidator
{
    private bool _isRunning;
    private double _progress;
    private long _runGeneration;

    /// <summary>
    ///     Creates a single-run tool presentation model.
    /// </summary>
    /// <param name="execution">Coordinates cancellation for the tool operation.</param>
    /// <param name="operationId">Stable identifier used by the execution service.</param>
    /// <exception cref="ArgumentNullException"><paramref name="execution" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="operationId" /> is empty or whitespace.</exception>
    protected SingleRunToolViewModel(
        IToolExecutionService execution,
        string operationId)
    {
        Execution = execution ?? throw new ArgumentNullException(nameof(execution));
        ExecutionOperationId = string.IsNullOrWhiteSpace(operationId)
            ? throw new ArgumentException("An operation identifier is required.", nameof(operationId))
            : operationId;

        RunCommand = new AsyncRelayCommand(RunAsync, CanRun);
        CancelCommand = new RelayCommand(Cancel);
    }

    /// <summary>Gets the stable execution identifier assigned to this tool.</summary>
    protected string ExecutionOperationId { get; }

    /// <summary>Gets the execution service shared by this tool's operations.</summary>
    protected IToolExecutionService Execution { get; }

    /// <summary>Gets whether the ordinary tool run is currently active.</summary>
    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (SetProperty(ref _isRunning, value)) RunCommand.NotifyCanExecuteChanged();
        }
    }

    /// <summary>Gets the current ordinary-run completion percentage.</summary>
    public double Progress
    {
        get => _progress;
        private set => SetProperty(ref _progress, value);
    }

    /// <summary>Gets the command that starts the tool's ordinary run.</summary>
    public IAsyncRelayCommand RunCommand { get; }

    /// <summary>Gets the command that requests cancellation of the active run.</summary>
    public IRelayCommand CancelCommand { get; }

    /// <summary>
    ///     Performs feature-specific validation before the run state is entered.
    /// </summary>
    /// <returns><see langword="true" /> when the ordinary run may start.</returns>
    protected virtual bool PrepareRun()
    {
        return true;
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
        Interlocked.Increment(ref _runGeneration);
        try
        {
            await operation();
        }
        finally
        {
            Interlocked.Increment(ref _runGeneration);
            Progress = 0;
            IsRunning = false;
        }
    }

    /// <summary>Creates a progress receiver that updates the shared progress property.</summary>
    /// <returns>A progress receiver for the tool execution service.</returns>
    protected IProgress<ToolExecutionProgress> CreateProgress()
    {
        long runGeneration = Volatile.Read(ref _runGeneration);
        return new Progress<ToolExecutionProgress>(value =>
        {
            if (IsRunning && Volatile.Read(ref _runGeneration) == runGeneration) Progress = value.Percent;
        });
    }

    private bool CanRun()
    {
        return !IsRunning;
    }

    private async Task RunAsync()
    {
        if (PrepareRun()) await RunWithStateAsync(RunCoreAsync);
    }

    private void Cancel()
    {
        Execution.Cancel(ExecutionOperationId);
    }
}
