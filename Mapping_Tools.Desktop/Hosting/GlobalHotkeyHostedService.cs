using Mapping_Tools.ApplicationServices.Backups;
using Mapping_Tools.ApplicationServices.QuickRun;
using Mapping_Tools.ApplicationServices.Settings;
using Microsoft.Extensions.Hosting;

namespace Mapping_Tools.Desktop.Hosting;

internal sealed class GlobalHotkeyHostedService : IHostedService
{
    private const string QuickRunBindingId = "quick-run";
    private const string QuickUndoBindingId = "quick-undo";
    private readonly IGlobalHotkeyService _hotkeys;
    private readonly IQuickRunService _quickRun;
    private readonly IQuickUndoCommandService _quickUndo;
    private readonly ApplicationSettings _settings;

    internal GlobalHotkeyHostedService(
        IGlobalHotkeyService hotkeys,
        IQuickRunService quickRun,
        IQuickUndoCommandService quickUndo,
        ApplicationSettings settings)
    {
        _hotkeys = hotkeys ?? throw new ArgumentNullException(nameof(hotkeys));
        _quickRun = quickRun ?? throw new ArgumentNullException(nameof(quickRun));
        _quickUndo = quickUndo ?? throw new ArgumentNullException(nameof(quickUndo));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _hotkeys.SetBinding(
            QuickRunBindingId,
            _settings.QuickRunHotkey,
            token => _quickRun.RunAsync(token));
        _hotkeys.SetBinding(
            QuickUndoBindingId,
            _settings.QuickUndoHotkey,
            token => _quickUndo.ExecuteAsync(token));
        _hotkeys.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _hotkeys.Stop();
        return Task.CompletedTask;
    }
}
