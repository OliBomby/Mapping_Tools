using Mapping_Tools.Application.Tools;

namespace Mapping_Tools.Application.QuickRun.Models;

/// <summary>
///     Defines a frontend-independent QuickRun entry without retaining a view,
///     control, or navigation object.
/// </summary>
public sealed class QuickRunCommand
{
    /// <summary>
    ///     Creates a command from the canonical tool metadata and a frontend-owned
    ///     execution callback.
    /// </summary>
    /// <param name="definition">The tool metadata containing its stable identity and QuickRun targets.</param>
    /// <param name="execute">The callback that performs the tool's QuickRun path.</param>
    /// <exception cref="ArgumentException">The tool has no QuickRun targets.</exception>
    public QuickRunCommand(
        ToolDefinition definition,
        Func<CancellationToken, Task> execute)
        : this(
            definition?.Id ?? throw new ArgumentNullException(nameof(definition)),
            definition.DisplayName,
            definition.QuickRunTargets
                ?? throw new ArgumentException(
                    "The tool definition does not declare QuickRun targets.",
                    nameof(definition)),
            execute)
    {
    }

    /// <summary>
    ///     Creates a command whose callback performs the same quick execution used
    ///     by an in-app button or global shortcut.
    /// </summary>
    /// <param name="id">A stable persistence and lookup key owned by the feature.</param>
    /// <param name="displayName">The legacy-compatible name shown in Smart QuickRun choices.</param>
    /// <param name="targets">The live selection sizes for which the command is a sensible explicit target.</param>
    /// <param name="execute">The feature callback; migrated tools should delegate their work to the shared execution service.</param>
    public QuickRunCommand(
        string id,
        string displayName,
        QuickRunTargets targets,
        Func<CancellationToken, Task> execute)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        if (targets == 0 || (targets & ~QuickRunTargets.Always) != 0)
            throw new ArgumentOutOfRangeException(
                nameof(targets),
                targets,
                "At least one known Smart QuickRun selection target is required.");

        Id = id;
        DisplayName = displayName;
        Targets = targets;
        Execute = execute ?? throw new ArgumentNullException(nameof(execute));
    }

    /// <summary>
    ///     Supplies the stable key used for current-command state and duplicate registration checks.
    /// </summary>
    public string Id { get; }

    /// <summary>
    ///     Preserves the user-facing tool name stored by legacy Smart QuickRun settings.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    ///     Limits where the command appears as a configurable smart target; it does
    ///     not prevent a user from invoking the command while it is current.
    /// </summary>
    public QuickRunTargets Targets { get; }

    /// <summary>
    ///     Invokes the feature's quick path with cooperative application-shutdown cancellation.
    /// </summary>
    public Func<CancellationToken, Task> Execute { get; }
}

