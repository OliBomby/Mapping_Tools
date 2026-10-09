using Mapping_Tools.Core.Settings.Models;

namespace Mapping_Tools.Infrastructure.Platform;

internal interface IGlobalShortcutPortal
{
    Task RunAsync(
        IReadOnlyDictionary<string, string?> shortcuts,
        IReadOnlyDictionary<string, HotkeySettings> updates,
        Action<string> activated,
        Action<Dictionary<string, HotkeySettings>, Func<CancellationToken, Task<Dictionary<string, HotkeySettings>>>> registered,
        CancellationToken cancellationToken);
}
