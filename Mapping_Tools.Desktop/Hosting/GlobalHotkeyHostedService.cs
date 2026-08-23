using Mapping_Tools.Application.Backups;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.QuickRun;
using Mapping_Tools.Application.Settings;
using Microsoft.Extensions.Hosting;

namespace Mapping_Tools.Desktop.Hosting;

internal sealed class GlobalHotkeyHostedService : IHostedService, IHotkeyBindingCoordinator
{
    private const string QuickRunBindingId = "quick-run";
    private const string QuickUndoBindingId = "quick-undo";
    private const string BetterSaveBindingId = "better-save";
    private readonly IBetterSaveService _betterSave;
    private readonly IGlobalHotkeyService _hotkeys;
    private readonly IQuickRunService _quickRun;
    private readonly IQuickUndoCommandService _quickUndo;
    private readonly ApplicationSettings _settings;

    public GlobalHotkeyHostedService(
        IGlobalHotkeyService hotkeys,
        IQuickRunService quickRun,
        IQuickUndoCommandService quickUndo,
        IBetterSaveService betterSave,
        ApplicationSettings settings)
    {
        _hotkeys = hotkeys ?? throw new ArgumentNullException(nameof(hotkeys));
        _quickRun = quickRun ?? throw new ArgumentNullException(nameof(quickRun));
        _quickUndo = quickUndo ?? throw new ArgumentNullException(nameof(quickUndo));
        _betterSave = betterSave ?? throw new ArgumentNullException(nameof(betterSave));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ApplyQuickRun(_settings.QuickRunHotkey);
        ApplyQuickUndo(_settings.QuickUndoHotkey);
        ApplyBetterSave(_settings.BetterSaveHotkey);
        _hotkeys.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _hotkeys.Stop();
        return Task.CompletedTask;
    }

    public void ApplyQuickRun(HotkeySettings? hotkey)
    {
        _hotkeys.SetBinding(
            QuickRunBindingId,
            hotkey,
            token => _quickRun.RunAsync(token));
    }

    public void ApplyQuickUndo(HotkeySettings? hotkey)
    {
        _hotkeys.SetBinding(
            QuickUndoBindingId,
            hotkey,
            token => _quickUndo.ExecuteAsync(token));
    }

    public void ApplyBetterSave(HotkeySettings? hotkey)
    {
        _hotkeys.SetBinding(
            BetterSaveBindingId,
            hotkey,
            token => _betterSave.ExecuteAsync(token));
    }
}
