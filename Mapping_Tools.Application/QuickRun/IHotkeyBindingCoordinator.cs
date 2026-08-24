using Mapping_Tools.Application.Settings;
using Mapping_Tools.Application.Tools;

namespace Mapping_Tools.Application.QuickRun;

/// <summary>
///     Applies live process-wide shortcut changes to the callbacks owned by the application.
/// </summary>
public interface IHotkeyBindingCoordinator
{
    /// <summary>Replaces or removes the global QuickRun binding.</summary>
    /// <param name="hotkey">Persisted legacy-compatible key data, or null to disable it.</param>
    void ApplyQuickRun(HotkeySettings? hotkey);

    /// <summary>Replaces or removes the global QuickUndo binding.</summary>
    /// <param name="hotkey">Persisted legacy-compatible key data, or null to disable it.</param>
    void ApplyQuickUndo(HotkeySettings? hotkey);

    /// <summary>Replaces or removes the global BetterSave binding.</summary>
    /// <param name="hotkey">Persisted legacy-compatible key data, or null to disable it.</param>
    void ApplyBetterSave(HotkeySettings? hotkey);
}
