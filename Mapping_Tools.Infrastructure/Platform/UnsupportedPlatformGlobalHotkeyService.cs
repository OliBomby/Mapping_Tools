using Mapping_Tools.Application.QuickRun.Contracts;
using Mapping_Tools.Core.Settings.Models;

namespace Mapping_Tools.Infrastructure.Platform;

/// <summary>
///     Ignores global shortcut registration on platforms without the Windows
///     keyboard-hook integration.
/// </summary>
public sealed class UnsupportedPlatformGlobalHotkeyService : IGlobalHotkeyService
{
    /// <inheritdoc />
    public void SetBinding(
        string id,
        HotkeySettings? hotkey,
        Func<CancellationToken, Task> callback)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(callback);
    }

    /// <inheritdoc />
    public void Start()
    {
    }

    /// <inheritdoc />
    public void Stop()
    {
    }
}
