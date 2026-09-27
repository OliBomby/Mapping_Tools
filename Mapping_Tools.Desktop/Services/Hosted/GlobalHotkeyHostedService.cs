using Mapping_Tools.Application.Backups.Contracts;
using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.QuickRun.Contracts;
using Mapping_Tools.Core.Settings.Models;
using Mapping_Tools.Desktop.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mapping_Tools.Desktop.Services.Hosted;

internal sealed class GlobalHotkeyHostedService : IHostedService, IHotkeyBindingCoordinator
{
    private const string quick_run_binding_id = "quick-run";
    private const string quick_undo_binding_id = "quick-undo";
    private const string better_save_binding_id = "better-save";
    private readonly IBetterSaveService betterSave;
    private readonly ILogger<GlobalHotkeyHostedService> logger;
    private readonly IGlobalHotkeyService hotkeys;
    private readonly IQuickRunService quickRun;
    private readonly IQuickUndoCommandService quickUndo;
    private readonly DesktopApplicationSettings settings;

    public GlobalHotkeyHostedService(
        IGlobalHotkeyService hotkeys,
        IQuickRunService quickRun,
        IQuickUndoCommandService quickUndo,
        IBetterSaveService betterSave,
        DesktopApplicationSettings settings,
        ILogger<GlobalHotkeyHostedService>? logger = null)
    {
        this.hotkeys = hotkeys ?? throw new ArgumentNullException(nameof(hotkeys));
        this.quickRun = quickRun ?? throw new ArgumentNullException(nameof(quickRun));
        this.quickUndo = quickUndo ?? throw new ArgumentNullException(nameof(quickUndo));
        this.betterSave = betterSave ?? throw new ArgumentNullException(nameof(betterSave));
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.logger = logger ?? NullLogger<GlobalHotkeyHostedService>.Instance;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ApplyQuickRun(settings.QuickRunHotkey);
        ApplyQuickUndo(settings.QuickUndoHotkey);
        ApplyBetterSave(settings.BetterSaveHotkey);
        hotkeys.Start();
        logger.LogInformation("Global hotkey listener started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        hotkeys.Stop();
        logger.LogInformation("Global hotkey listener stopped");
        return Task.CompletedTask;
    }

    public void ApplyQuickRun(HotkeySettings? hotkey)
    {
        logger.LogInformation("QuickRun hotkey set to key {Key} modifiers {Modifiers}", hotkey?.Key, hotkey?.Modifiers);
        hotkeys.SetBinding(
            quick_run_binding_id,
            hotkey,
            token =>
            {
                logger.LogInformation("User pressed QuickRun hotkey");
                return quickRun.RunAsync(token);
            });
    }

    public void ApplyQuickUndo(HotkeySettings? hotkey)
    {
        logger.LogInformation("QuickUndo hotkey set to key {Key} modifiers {Modifiers}", hotkey?.Key, hotkey?.Modifiers);
        hotkeys.SetBinding(
            quick_undo_binding_id,
            hotkey,
            token =>
            {
                logger.LogInformation("User pressed QuickUndo hotkey");
                return quickUndo.ExecuteAsync(token);
            });
    }

    public void ApplyBetterSave(HotkeySettings? hotkey)
    {
        logger.LogInformation("BetterSave hotkey set to key {Key} modifiers {Modifiers}", hotkey?.Key, hotkey?.Modifiers);
        hotkeys.SetBinding(
            better_save_binding_id,
            hotkey,
            token =>
            {
                logger.LogInformation("User pressed BetterSave hotkey");
                return betterSave.ExecuteAsync(token);
            });
    }
}
