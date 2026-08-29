using Mapping_Tools.Application.Backups.Contracts;
using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.QuickRun.Contracts;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Core.Settings.Models;
using Microsoft.Extensions.Hosting;

namespace Mapping_Tools.Desktop.Hosting;

internal sealed class GlobalHotkeyHostedService : IHostedService, IHotkeyBindingCoordinator
{
    private const string quick_run_binding_id = "quick-run";
    private const string quick_undo_binding_id = "quick-undo";
    private const string better_save_binding_id = "better-save";
    private readonly IBetterSaveService betterSave;
    private readonly IGlobalHotkeyService hotkeys;
    private readonly IQuickRunService quickRun;
    private readonly IQuickUndoCommandService quickUndo;
    private readonly ApplicationSettings settings;

    public GlobalHotkeyHostedService(
        IGlobalHotkeyService hotkeys,
        IQuickRunService quickRun,
        IQuickUndoCommandService quickUndo,
        IBetterSaveService betterSave,
        ApplicationSettings settings)
    {
        this.hotkeys = hotkeys ?? throw new ArgumentNullException(nameof(hotkeys));
        this.quickRun = quickRun ?? throw new ArgumentNullException(nameof(quickRun));
        this.quickUndo = quickUndo ?? throw new ArgumentNullException(nameof(quickUndo));
        this.betterSave = betterSave ?? throw new ArgumentNullException(nameof(betterSave));
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ApplyQuickRun(settings.QuickRunHotkey);
        ApplyQuickUndo(settings.QuickUndoHotkey);
        ApplyBetterSave(settings.BetterSaveHotkey);
        hotkeys.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        hotkeys.Stop();
        return Task.CompletedTask;
    }

    public void ApplyQuickRun(HotkeySettings? hotkey)
    {
        hotkeys.SetBinding(
            quick_run_binding_id,
            hotkey,
            token => quickRun.RunAsync(token));
    }

    public void ApplyQuickUndo(HotkeySettings? hotkey)
    {
        hotkeys.SetBinding(
            quick_undo_binding_id,
            hotkey,
            token => quickUndo.ExecuteAsync(token));
    }

    public void ApplyBetterSave(HotkeySettings? hotkey)
    {
        hotkeys.SetBinding(
            better_save_binding_id,
            hotkey,
            token => betterSave.ExecuteAsync(token));
    }
}
