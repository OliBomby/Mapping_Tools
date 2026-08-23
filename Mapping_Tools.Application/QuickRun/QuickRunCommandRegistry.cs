namespace Mapping_Tools.Application.QuickRun;

/// <summary>
///     Maintains a deterministic process-lifetime QuickRun catalog without
///     constructing feature views merely to discover their capabilities.
/// </summary>
public sealed class QuickRunCommandRegistry : IQuickRunCommandRegistry
{
    private readonly List<QuickRunCommand> commands = [];
    private readonly object gate = new();
    private string? currentCommandId;

    /// <inheritdoc />
    public IReadOnlyList<QuickRunCommand> Commands
    {
        get
        {
            lock (gate)
            {
                return commands.ToArray();
            }
        }
    }

    /// <inheritdoc />
    public string? CurrentCommandId
    {
        get
        {
            lock (gate)
            {
                return currentCommandId;
            }
        }
    }

    /// <inheritdoc />
    public void Register(QuickRunCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        lock (gate)
        {
            if (commands.Any(existing =>
                    string.Equals(existing.Id, command.Id, StringComparison.Ordinal)
                    || string.Equals(
                        existing.DisplayName,
                        command.DisplayName,
                        StringComparison.Ordinal)))
                throw new InvalidOperationException(
                    $"QuickRun command '{command.Id}' or display name " + $"'{command.DisplayName}' is already registered.");

            commands.Add(command);
        }
    }

    /// <inheritdoc />
    public bool Remove(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        lock (gate)
        {
            int index = commands.FindIndex(command => string.Equals(command.Id, id, StringComparison.Ordinal));
            if (index < 0) return false;

            commands.RemoveAt(index);
            if (string.Equals(currentCommandId, id, StringComparison.Ordinal)) currentCommandId = null;

            return true;
        }
    }

    /// <inheritdoc />
    public bool SelectCurrent(string? id)
    {
        lock (gate)
        {
            if (id is not null
                && !commands.Any(command =>
                    string.Equals(command.Id, id, StringComparison.Ordinal)))
                return false;

            currentCommandId = id;
            return true;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<QuickRunCommand> GetCommandsFor(QuickRunTargets target)
    {
        if (target is not QuickRunTargets.NoSelection and
            not QuickRunTargets.SingleSelection and
            not QuickRunTargets.MultipleSelection)
            throw new ArgumentOutOfRangeException(
                nameof(target),
                target,
                "Exactly one live selection-size target is required.");

        lock (gate)
        {
            return commands
                .Where(command => (command.Targets & target) != 0)
                .ToArray();
        }
    }

    internal QuickRunCommand? FindCurrent()
    {
        lock (gate)
        {
            return commands.FirstOrDefault(command =>
                string.Equals(command.Id, currentCommandId, StringComparison.Ordinal));
        }
    }

    internal QuickRunCommand? FindByDisplayName(string displayName)
    {
        lock (gate)
        {
            return commands.FirstOrDefault(command =>
                string.Equals(
                    command.DisplayName,
                    displayName,
                    StringComparison.Ordinal));
        }
    }
}
