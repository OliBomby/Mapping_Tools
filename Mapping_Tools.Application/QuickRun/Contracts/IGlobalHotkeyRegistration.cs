using Mapping_Tools.Core.Settings.Models;

namespace Mapping_Tools.Application.QuickRun.Contracts;

/// <summary>Exposes shortcuts granted by a desktop that owns global shortcut registration.</summary>
public interface IGlobalHotkeyRegistration
{
    /// <summary>Reports registration or refresh failures, including shortcuts left unassigned by the desktop.</summary>
    event EventHandler<Exception>? RegistrationFailed;

    /// <summary>Reads the current desktop assignments without changing them or requesting permission again.</summary>
    /// <param name="cancellationToken">Cancels waiting for registration or the desktop's response.</param>
    /// <returns>Binding IDs mapped to desktop assignments; a zero key denotes an unassigned shortcut.</returns>
    /// <exception cref="OperationCanceledException">The refresh was cancelled or its desktop session changed.</exception>
    Task<Dictionary<string, HotkeySettings>> GetRegisteredShortcutsAsync(CancellationToken cancellationToken);
}
