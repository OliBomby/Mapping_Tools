namespace Mapping_Tools.Application.QuickRun;

/// <summary>
/// Maintains a deterministic process-lifetime QuickRun catalog without
/// constructing feature views merely to discover their capabilities.
/// </summary>
public sealed class QuickRunCommandRegistry : IQuickRunCommandRegistry
{
    private readonly object _gate = new();
    private readonly List<QuickRunCommand> _commands = [];
    private string? _currentCommandId;

    /// <inheritdoc/>
    public IReadOnlyList<QuickRunCommand> Commands
    {
        get
        {
            lock (_gate)
            {
                return _commands.ToArray();
            }
        }
    }

    /// <inheritdoc/>
    public string? CurrentCommandId
    {
        get
        {
            lock (_gate)
            {
                return _currentCommandId;
            }
        }
    }

    /// <inheritdoc/>
    public void Register(QuickRunCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        lock (_gate)
        {
            if (_commands.Any(existing =>
                    string.Equals(existing.Id, command.Id, StringComparison.Ordinal) ||
                    string.Equals(
                        existing.DisplayName,
                        command.DisplayName,
                        StringComparison.Ordinal)))
            {
                throw new InvalidOperationException(
                    $"QuickRun command '{command.Id}' or display name " +
                    $"'{command.DisplayName}' is already registered.");
            }

            _commands.Add(command);
        }
    }

    /// <inheritdoc/>
    public bool Remove(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        lock (_gate)
        {
            int index = _commands.FindIndex(
                command => string.Equals(command.Id, id, StringComparison.Ordinal));
            if (index < 0)
            {
                return false;
            }

            _commands.RemoveAt(index);
            if (string.Equals(_currentCommandId, id, StringComparison.Ordinal))
            {
                _currentCommandId = null;
            }

            return true;
        }
    }

    /// <inheritdoc/>
    public bool SelectCurrent(string? id)
    {
        lock (_gate)
        {
            if (id is not null &&
                !_commands.Any(command =>
                    string.Equals(command.Id, id, StringComparison.Ordinal)))
            {
                return false;
            }

            _currentCommandId = id;
            return true;
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<QuickRunCommand> GetCommandsFor(QuickRunTargets target)
    {
        if (target is not QuickRunTargets.NoSelection and
            not QuickRunTargets.SingleSelection and
            not QuickRunTargets.MultipleSelection)
        {
            throw new ArgumentOutOfRangeException(
                nameof(target),
                target,
                "Exactly one live selection-size target is required.");
        }

        lock (_gate)
        {
            return _commands
                .Where(command => (command.Targets & target) != 0)
                .ToArray();
        }
    }

    internal QuickRunCommand? FindCurrent()
    {
        lock (_gate)
        {
            return _commands.FirstOrDefault(command =>
                string.Equals(command.Id, _currentCommandId, StringComparison.Ordinal));
        }
    }

    internal QuickRunCommand? FindByDisplayName(string displayName)
    {
        lock (_gate)
        {
            return _commands.FirstOrDefault(command =>
                string.Equals(
                    command.DisplayName,
                    displayName,
                    StringComparison.Ordinal));
        }
    }
}
