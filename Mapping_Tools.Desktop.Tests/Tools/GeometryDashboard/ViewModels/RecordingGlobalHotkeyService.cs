using Mapping_Tools.Application.QuickRun.Contracts;
using Mapping_Tools.Core.Settings.Models;

namespace Mapping_Tools.Desktop.Tests.Tools.GeometryDashboard.ViewModels;

internal sealed class RecordingGlobalHotkeyService : IGlobalHotkeyService
{
    public Dictionary<string, HotkeySettings?> Bindings { get; } = new(StringComparer.Ordinal);

    public Dictionary<string, Func<CancellationToken, Task>> Callbacks { get; } = new(StringComparer.Ordinal);

    public void SetBinding(string id, HotkeySettings? hotkey, Func<CancellationToken, Task> callback)
    {
        Bindings[id] = hotkey;
        Callbacks[id] = callback;
    }

    public void Start() { }

    public void Stop() { }
}
