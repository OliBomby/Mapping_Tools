using Mapping_Tools.Core.Settings.Models;

namespace Mapping_Tools.Application.QuickRun.Contracts;

/// <summary>
///     Abstracts process-wide keyboard registration so Application can bind
///     commands without depending on a Windows hook library.
/// </summary>
public interface IGlobalHotkeyService
{
    /// <summary>
    ///     Adds or replaces a named binding before or after the platform listener starts.
    ///     A null or disabled key removes the binding.
    /// </summary>
    /// <param name="id">A stable owner key used for later replacement.</param>
    /// <param name="hotkey">Legacy-compatible key data, or <see langword="null" /> to unbind.</param>
    /// <param name="callback">Work scheduled when the key combination is pressed globally.</param>
    void SetBinding(
        string id,
        HotkeySettings? hotkey,
        Func<CancellationToken, Task> callback);

    /// <summary>
    ///     Activates all configured bindings and begins observing global keyboard input.
    /// </summary>
    void Start();

    /// <summary>
    ///     Unregisters every binding and releases platform listener state.
    /// </summary>
    void Stop();
}
