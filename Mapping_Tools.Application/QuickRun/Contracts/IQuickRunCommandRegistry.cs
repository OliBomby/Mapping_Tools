using Mapping_Tools.Application.QuickRun.Models;

namespace Mapping_Tools.Application.QuickRun.Contracts;

/// <summary>
///     Owns the explicit command catalog and current in-app command selection that
///     replace reflection over instantiated WPF views.
/// </summary>
public interface IQuickRunCommandRegistry
{
    /// <summary>
    ///     Returns an immutable snapshot in registration order for preference and navigation surfaces.
    /// </summary>
    IReadOnlyList<QuickRunCommand> Commands { get; }

    /// <summary>
    ///     Identifies the command selected by the shell, or <see langword="null" />
    ///     before a QuickRun-capable feature becomes current.
    /// </summary>
    string? CurrentCommandId { get; }

    /// <summary>
    ///     Adds one feature command while rejecting ambiguous identifiers and legacy display names.
    /// </summary>
    /// <param name="command">The feature-owned command to add.</param>
    /// <exception cref="InvalidOperationException">Its identifier or display name is already registered.</exception>
    void Register(QuickRunCommand command);

    /// <summary>
    ///     Removes a feature command and clears current selection when that command was active.
    /// </summary>
    /// <param name="id">The stable identifier supplied during registration.</param>
    /// <returns>Whether a registered command was removed.</returns>
    bool Remove(string id);

    /// <summary>
    ///     Changes the current in-app command without invoking it.
    /// </summary>
    /// <param name="id">A registered command identifier, or <see langword="null" /> to clear selection.</param>
    /// <returns><see langword="false" /> only when a non-null identifier is not registered.</returns>
    bool SelectCurrent(string? id);

    /// <summary>
    ///     Returns commands suitable for a particular live selection size in registration order.
    /// </summary>
    /// <param name="target">Exactly one zero, one, or multiple-selection flag.</param>
    /// <returns>An immutable snapshot used to populate Smart QuickRun choices.</returns>
    IReadOnlyList<QuickRunCommand> GetCommandsFor(QuickRunTargets target);
}

