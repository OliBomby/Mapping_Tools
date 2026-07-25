using Mapping_Tools.ApplicationServices.Settings;

namespace Mapping_Tools.ApplicationServices.QuickRun;

/// <summary>
/// Identifies the live osu! selection sizes for which a command may be offered
/// as an explicit Smart QuickRun target.
/// </summary>
[Flags]
public enum QuickRunTargets
{
    /// <summary>
    /// The editor has no selected hit objects.
    /// </summary>
    NoSelection = 1,

    /// <summary>
    /// The editor has exactly one selected hit object.
    /// </summary>
    SingleSelection = 1 << 1,

    /// <summary>
    /// The editor has at least two selected hit objects.
    /// </summary>
    MultipleSelection = 1 << 2,

    /// <summary>
    /// The command accepts either one or multiple selected hit objects.
    /// </summary>
    AnySelection = SingleSelection | MultipleSelection,

    /// <summary>
    /// The command is suitable for every live selection size.
    /// </summary>
    Always = NoSelection | SingleSelection | MultipleSelection
}

/// <summary>
/// Defines a frontend-independent QuickRun entry without retaining a view,
/// control, or navigation object.
/// </summary>
public sealed class QuickRunCommand
{
    /// <summary>
    /// Creates a command whose callback performs the same quick execution used
    /// by an in-app button or global shortcut.
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
        {
            throw new ArgumentOutOfRangeException(
                nameof(targets),
                targets,
                "At least one known Smart QuickRun selection target is required.");
        }

        Id = id;
        DisplayName = displayName;
        Targets = targets;
        Execute = execute ?? throw new ArgumentNullException(nameof(execute));
    }

    /// <summary>
    /// Supplies the stable key used for current-command state and duplicate registration checks.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Preserves the user-facing tool name stored by legacy Smart QuickRun settings.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Limits where the command appears as a configurable smart target; it does
    /// not prevent a user from invoking the command while it is current.
    /// </summary>
    public QuickRunTargets Targets { get; }

    /// <summary>
    /// Invokes the feature's quick path with cooperative application-shutdown cancellation.
    /// </summary>
    public Func<CancellationToken, Task> Execute { get; }
}

/// <summary>
/// Owns the explicit command catalog and current in-app command selection that
/// replace reflection over instantiated WPF views.
/// </summary>
public interface IQuickRunCommandRegistry
{
    /// <summary>
    /// Returns an immutable snapshot in registration order for preference and navigation surfaces.
    /// </summary>
    IReadOnlyList<QuickRunCommand> Commands { get; }

    /// <summary>
    /// Identifies the command selected by the shell, or <see langword="null"/>
    /// before a QuickRun-capable feature becomes current.
    /// </summary>
    string? CurrentCommandId { get; }

    /// <summary>
    /// Adds one feature command while rejecting ambiguous identifiers and legacy display names.
    /// </summary>
    /// <param name="command">The feature-owned command to add.</param>
    /// <exception cref="InvalidOperationException">Its identifier or display name is already registered.</exception>
    void Register(QuickRunCommand command);

    /// <summary>
    /// Removes a feature command and clears current selection when that command was active.
    /// </summary>
    /// <param name="id">The stable identifier supplied during registration.</param>
    /// <returns>Whether a registered command was removed.</returns>
    bool Remove(string id);

    /// <summary>
    /// Changes the current in-app command without invoking it.
    /// </summary>
    /// <param name="id">A registered command identifier, or <see langword="null"/> to clear selection.</param>
    /// <returns><see langword="false"/> only when a non-null identifier is not registered.</returns>
    bool SelectCurrent(string? id);

    /// <summary>
    /// Returns commands suitable for a particular live selection size in registration order.
    /// </summary>
    /// <param name="target">Exactly one zero, one, or multiple-selection flag.</param>
    /// <returns>An immutable snapshot used to populate Smart QuickRun choices.</returns>
    IReadOnlyList<QuickRunCommand> GetCommandsFor(QuickRunTargets target);
}

/// <summary>
/// Distinguishes successful QuickRun dispatch from configuration, live-editor,
/// and command failures without requiring a view-owned completion event.
/// </summary>
public enum QuickRunStatus
{
    /// <summary>
    /// The resolved command callback completed successfully.
    /// </summary>
    Executed,

    /// <summary>
    /// Current-tool routing was requested before the shell selected a capable command.
    /// </summary>
    NoCurrentCommand,

    /// <summary>
    /// A persisted Smart QuickRun tool name no longer exists in the command catalog.
    /// </summary>
    CommandNotFound,

    /// <summary>
    /// Smart routing could not inspect the currently open osu! beatmap.
    /// </summary>
    EditorUnavailable,

    /// <summary>
    /// Live selection discovery or the resolved command threw an exception.
    /// </summary>
    Failed
}

/// <summary>
/// Reports the command selected by QuickRun together with any diagnostic that
/// prevented it from completing.
/// </summary>
/// <param name="Status">The terminal dispatch outcome.</param>
/// <param name="CommandId">The resolved stable command key, when resolution reached a command.</param>
/// <param name="Exception">The live-read or command failure captured for diagnostics.</param>
public sealed record QuickRunResult(
    QuickRunStatus Status,
    string? CommandId = null,
    Exception? Exception = null);

/// <summary>
/// Resolves and invokes the current or configured Smart QuickRun command from
/// live osu! selection state.
/// </summary>
public interface IQuickRunService
{
    /// <summary>
    /// Applies the current settings, resolves one command, and awaits its quick callback.
    /// </summary>
    /// <param name="cancellationToken">Cancels live selection discovery or the feature callback.</param>
    /// <returns>A typed dispatch outcome; ordinary live-read and command failures are captured.</returns>
    Task<QuickRunResult> RunAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Abstracts process-wide keyboard registration so Application can bind
/// commands without depending on a Windows hook library.
/// </summary>
public interface IGlobalHotkeyService
{
    /// <summary>
    /// Adds or replaces a named binding before or after the platform listener starts.
    /// A null or disabled key removes the binding.
    /// </summary>
    /// <param name="id">A stable owner key used for later replacement.</param>
    /// <param name="hotkey">Legacy-compatible key data, or <see langword="null"/> to unbind.</param>
    /// <param name="callback">Work scheduled when the key combination is pressed globally.</param>
    void SetBinding(
        string id,
        HotkeySettings? hotkey,
        Func<CancellationToken, Task> callback);

    /// <summary>
    /// Activates all configured bindings and begins observing global keyboard input.
    /// </summary>
    void Start();

    /// <summary>
    /// Unregisters every binding and releases platform listener state.
    /// </summary>
    void Stop();
}
