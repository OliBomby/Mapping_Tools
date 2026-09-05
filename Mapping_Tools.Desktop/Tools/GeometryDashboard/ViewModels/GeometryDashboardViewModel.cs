using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Execution.UserNotification.Models;
using Mapping_Tools.Application.Platform.FilePicker;
using Mapping_Tools.Application.Projects.Contracts;
using Mapping_Tools.Application.Projects.Models;
using Mapping_Tools.Application.QuickRun.Contracts;
using Mapping_Tools.Application.Tools.GeometryDashboard.Contracts;
using Mapping_Tools.Application.Tools.GeometryDashboard.Models;
using Mapping_Tools.Application.Tools.GeometryDashboard;
using Mapping_Tools.Core.Settings.Models;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorTypes;
using Mapping_Tools.Core.Tools.GeometryDashboard.Serialization;
using Mapping_Tools.Desktop.Models;
using Mapping_Tools.Desktop.Shell;
using Mapping_Tools.Desktop.Tools.GeometryDashboard.Interactions;
using Mapping_Tools.Desktop.Tools.GeometryDashboard.Models;
using Mapping_Tools.Desktop.Tools.GeometryDashboard.Views;
using Material.Icons;

namespace Mapping_Tools.Desktop.Tools.GeometryDashboard.ViewModels;

/// <summary>
///     Adapts the application Geometry Dashboard session to Avalonia bindings,
///     shell project persistence, dialogs, and Desktop hotkeys.
/// </summary>
public sealed partial class GeometryDashboardViewModel : ObservableObject,
    IShellProjectFeature<GeometryDashboardProject>, IShellExtraProjectMenuFeature, IShellFeatureActivation, IDisposable
{
    private const string save_slot_binding_prefix = "geometry-dashboard-save-slot";
    private readonly Dictionary<GeometryDashboardSaveSlot, string> saveSlotBindingIds = [];
    private readonly ProjectDefinition<GeometryDashboardProject> definition = new(
        "geometrydashboardproject.json",
        "Geometry Dashboard Projects",
        static () => new GeometryDashboardProject(),
        "geometry-dashboard-project.json",
        ToolConfigSchema.ForTool(GeometryDashboardToolDefinition.Definition.Id));
    private readonly IUiDispatcher dispatcher;
    private readonly IFilePicker filePicker;
    private readonly IGlobalHotkeyService globalHotkeys;
    private readonly ITextFileStore files;
    private readonly GeometryDashboardLifecycleCoordinator lifecycle;
    private readonly IUserNotificationService notifications;
    private readonly IGeometryDashboardService dashboardService;
    private readonly IProjectSerializer serializer;
    private readonly Func<Window> owner;
    private bool disposed;
    private string filter = string.Empty;
    private bool viewActive;

    /// <summary>Creates the dashboard presentation over an application session.</summary>
    /// <param name="project">The Desktop-owned project state.</param>
    /// <param name="dashboardService">The application calculation session.</param>
    /// <param name="lifecycle">The Desktop service lifecycle policy.</param>
    /// <param name="globalHotkeys">Registers discrete process-wide save-slot commands.</param>
    /// <param name="serializer">Reads and writes legacy-compatible JSON.</param>
    /// <param name="filePicker">Presents locked-object import/export pickers.</param>
    /// <param name="files">Reads and writes virtual-object files.</param>
    /// <param name="notifications">Publishes user-visible operation outcomes.</param>
    /// <param name="owner">Returns the shell window that owns dashboard dialogs.</param>
    /// <param name="dispatcher">Marshals observable state changes to Avalonia's UI thread.</param>
    public GeometryDashboardViewModel(
        GeometryDashboardProject project,
        IGeometryDashboardService dashboardService,
        GeometryDashboardLifecycleCoordinator lifecycle,
        IGlobalHotkeyService globalHotkeys,
        IProjectSerializer serializer,
        IFilePicker filePicker,
        ITextFileStore files,
        IUserNotificationService notifications,
        Func<Window> owner,
        IUiDispatcher dispatcher)
    {
        Project = project ?? throw new ArgumentNullException(nameof(project));
        this.dashboardService = dashboardService ?? throw new ArgumentNullException(nameof(dashboardService));
        this.lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
        this.globalHotkeys = globalHotkeys ?? throw new ArgumentNullException(nameof(globalHotkeys));
        this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        this.filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        this.files = files ?? throw new ArgumentNullException(nameof(files));
        this.notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));

        Generators = new ObservableCollection<GeometryDashboardGeneratorViewModel>(
            dashboardService.Generators.Select(generator => new GeometryDashboardGeneratorViewModel(generator, this)));
        RebuildGroups();
        dashboardService.StateChanged += OnDashboardStateChanged;
    }

    /// <summary>Gets the serializable project currently edited by the dashboard.</summary>
    public GeometryDashboardProject Project { get; }

    /// <summary>Gets generator rows in the legacy reflection order.</summary>
    public ObservableCollection<GeometryDashboardGeneratorViewModel> Generators { get; }

    /// <summary>Gets filtered generator groups for the dashboard list.</summary>
    public ObservableCollection<GeometryDashboardGeneratorGroupViewModel> GeneratorGroups { get; } = [];

    /// <summary>Gets the current connection, validation, or empty-state message.</summary>
    [ObservableProperty]
    public partial string Status { get; private set; } = "Waiting for osu!...";

    /// <summary>Gets or sets the case-insensitive generator search query.</summary>
    public string Filter
    {
        get => filter;
        set
        {
            string normalized = value ?? string.Empty;
            if (SetProperty(ref filter, normalized)) RebuildGroups();
        }
    }

    /// <summary>Gets the current engine preferences edited by the dashboard.</summary>
    public GeometryDashboardPreferences Preferences => Project.CurrentPreferences;

    /// <summary>Gets the generated geometry count displayed in diagnostics.</summary>
    public int DrawableCount => dashboardService.State.DrawableCount;

    /// <summary>Gets the number of selected virtual objects.</summary>
    public int SelectedCount => dashboardService.State.SelectedCount;

    /// <summary>Gets whether a live editor snapshot is currently displayed.</summary>
    public bool IsConnected => dashboardService.State.IsConnected;

    /// <summary>Gets or sets whether Desktop keeps the service running when this view is hidden.</summary>
    public bool KeepRunning => Project.KeepRunning;

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        dashboardService.StateChanged -= OnDashboardStateChanged;
        viewActive = false;
        lifecycle.ViewDeactivated();
        SynchronizeSaveSlotHotkeys();
        GC.SuppressFinalize(this);
    }

    IReadOnlyList<ShellProjectMenuItem> IShellExtraProjectMenuFeature.ExtraProjectMenuItems =>
    [
        new("_Save virtual objects", "Save locked virtual objects to a file.", SaveLockedObjectsCommand, MaterialIconKind.ContentSaveOutline),
        new("_Load virtual objects", "Load locked virtual objects from a save file.", LoadLockedObjectsCommand, MaterialIconKind.FolderOpen),
    ];

    /// <inheritdoc />
    public void Activate()
    {
        if (disposed) return;
        viewActive = true;
        lifecycle.ViewActivated();
        SynchronizeSaveSlotHotkeys();
    }

    /// <inheritdoc />
    public void Deactivate()
    {
        if (disposed) return;
        viewActive = false;
        lifecycle.ViewDeactivated();
        SynchronizeSaveSlotHotkeys();
    }

    ProjectDefinition<GeometryDashboardProject> IShellProjectFeature<GeometryDashboardProject>.ProjectDefinition => definition;

    GeometryDashboardProject IShellProjectFeature<GeometryDashboardProject>.Snapshot()
    {
        lock (Project)
        {
            Project.GetThis();
            return Project;
        }
    }

    void IShellProjectFeature<GeometryDashboardProject>.Install(GeometryDashboardProject project)
    {
        ArgumentNullException.ThrowIfNull(project);
        lock (Project)
        {
            Project.KeepRunning = project.KeepRunning;
            Project.SaveSlots.Clear();
            foreach (var slot in project.SaveSlots) Project.SaveSlots.Add(slot);
            Project.SetCurrentPreferences(project.CurrentPreferences);
        }

        dashboardService.ApplyPreferences();
        lifecycle.KeepRunningChanged();
        OnPropertyChanged(nameof(KeepRunning));
        SynchronizeSaveSlotHotkeys();
    }

    /// <summary>Runs one external-state update. This is public for focused UI behavior tests.</summary>
    /// <param name="cancellationToken">Cancels the adapter read and calculation.</param>
    /// <returns>A task that completes after the state has been reconciled.</returns>
    public async Task RefreshOnceAsync(CancellationToken cancellationToken = default)
    {
        await dashboardService.RefreshOnceAsync(cancellationToken).ConfigureAwait(false);
        ApplyDashboardState(dashboardService.State);
    }

    /// <summary>Executes the selection toggle using the legacy Shift/Ctrl modifiers.</summary>
    /// <param name="modifiers">The modifiers captured from the dashboard button press.</param>
    public void ToggleSelected(KeyModifiers modifiers = KeyModifiers.None)
    {
        dashboardService.ToggleSelected(ToTargetingMode(modifiers));
        ApplyDashboardState(dashboardService.State);
    }

    /// <summary>Executes the lock toggle using the legacy Shift/Ctrl modifiers.</summary>
    /// <param name="modifiers">The modifiers captured from the dashboard button press.</param>
    public void ToggleLocked(KeyModifiers modifiers = KeyModifiers.None)
    {
        dashboardService.ToggleLocked(ToTargetingMode(modifiers));
        ApplyDashboardState(dashboardService.State);
    }

    /// <summary>Executes the inheritable toggle using the legacy Shift/Ctrl modifiers.</summary>
    /// <param name="modifiers">The modifiers captured from the dashboard button press.</param>
    public void ToggleInheritable(KeyModifiers modifiers = KeyModifiers.None)
    {
        dashboardService.ToggleInheritable(ToTargetingMode(modifiers));
        ApplyDashboardState(dashboardService.State);
    }

    /// <summary>Shows the preferences dialog and applies an accepted clone.</summary>
    public async Task ShowPreferencesAsync()
    {
        GeometryDashboardPreferences preferences;
        bool keepRunning;
        lock (Project)
        {
            preferences = (GeometryDashboardPreferences)Preferences.Clone();
            keepRunning = Project.KeepRunning;
        }

        GeometryDashboardPreferencesDialogViewModel viewModel = new(preferences, keepRunning);
        GeometryDashboardPreferencesWindow window = new() { DataContext = viewModel };
        viewModel.Close = result => window.Close(result);
        var result = await window.ShowDialog<GeometryDashboardPreferencesDialogResult?>(owner());
        if (result is null) return;

        lock (Project)
        {
            Project.SetCurrentPreferences(result.Preferences);
            Project.KeepRunning = result.KeepRunning;
        }

        dashboardService.ApplyPreferences();
        lifecycle.KeepRunningChanged();
        OnPropertyChanged(nameof(KeepRunning));
        SynchronizeSaveSlotHotkeys();
    }

    /// <summary>Shows the modeless save-slot dialog for the current project.</summary>
    public Task ShowProjectSlotsAsync()
    {
        GeometryDashboardProjectSlotsViewModel viewModel = new(Project, LoadSaveSlot, RefreshSaveSlotHotkeys);
        GeometryDashboardProjectWindow window = new() { DataContext = viewModel };
        viewModel.Close = window.Close;
        window.Show(owner());
        return Task.CompletedTask;
    }

    /// <summary>Shows a generator's typed settings dialog and regenerates after acceptance.</summary>
    /// <param name="generator">The generator row requesting configuration.</param>
    public async Task ShowGeneratorSettingsAsync(GeometryDashboardGeneratorViewModel generator)
    {
        ArgumentNullException.ThrowIfNull(generator);
        GeometryDashboardGeneratorSettingsDialogViewModel viewModel = new(generator.Model.Settings);
        GeometryDashboardGeneratorSettingsWindow window = new() { DataContext = viewModel };
        viewModel.Close = result => window.Close(result);
        object? result = await window.ShowDialog<object?>(owner());
        if (result is true) dashboardService.Regenerate();
    }

    /// <summary>Exports detached locked virtual objects using a native save picker.</summary>
    [RelayCommand]
    private async Task SaveLockedObjectsAsync()
    {
        try
        {
            string? path = await filePicker.PickSaveFileAsync(new SaveFilePickerRequest
            {
                Title = "Save locked virtual objects",
                SuggestedFileName = "locked-virtual-objects.json",
                DefaultExtension = ".json",
                Filters = [CommonFilePickerFilters.MappingToolsProjects],
            });
            if (string.IsNullOrWhiteSpace(path)) return;

            string json = serializer.Serialize(
                definition.ConfigSchema,
                dashboardService.GetLockedObjects());
            files.WriteAllLines(path, json.Split(["\r\n", "\n"], StringSplitOptions.None));
            await notifications.PublishAsync(new UserNotification(
                UserNotificationSeverity.Success,
                "Save virtual objects",
                "Successfully saved locked virtual objects!"));
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            await notifications.PublishAsync(new UserNotification(
                UserNotificationSeverity.Error,
                "Could not save virtual objects",
                exception.Message,
                exception));
        }
    }

    /// <summary>Imports detached locked virtual objects using a native open picker.</summary>
    [RelayCommand]
    private async Task LoadLockedObjectsAsync()
    {
        try
        {
            var paths = await filePicker.PickOpenFilesAsync(new OpenFilePickerRequest
            {
                Title = "Load locked virtual objects",
                AllowMultiple = false,
                Filters = [CommonFilePickerFilters.MappingToolsProjects],
            });
            if (paths.Count == 0) return;

            var objects = serializer.Deserialize<RelevantObjectCollection>(
                definition.ConfigSchema,
                string.Join(Environment.NewLine, files.ReadAllLines(paths[0])));
            dashboardService.SetLockedObjects(objects);
            ApplyDashboardState(dashboardService.State);
            await notifications.PublishAsync(new UserNotification(
                UserNotificationSeverity.Success,
                "Load virtual objects",
                "Successfully loaded locked virtual objects!"));
        }
        catch (ArgumentException)
        {
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            await notifications.PublishAsync(new UserNotification(
                UserNotificationSeverity.Error,
                "Could not load virtual objects",
                exception.Message,
                exception));
        }
    }

    private void OnDashboardStateChanged(object? sender, EventArgs eventArgs)
    {
        ApplyDashboardState(dashboardService.State);
    }

    private void ApplyDashboardState(GeometryDashboardServiceState state)
    {
        dispatcher.Post(() =>
        {
            if (disposed) return;
            Status = state.Status;
            OnPropertyChanged(nameof(DrawableCount));
            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(IsConnected));
        });
    }

    private void LoadSaveSlot(GeometryDashboardSaveSlot slot)
    {
        dispatcher.Post(() =>
        {
            if (disposed) return;
            lock (Project) Project.LoadFromSlot(slot);
            dashboardService.ApplyPreferences();
        });
    }

    private void SynchronizeSaveSlotHotkeys()
    {
        GeometryDashboardSaveSlot[] slots;
        lock (Project) slots = Project.SaveSlots.ToArray();

        var currentSlots = slots.ToHashSet();
        foreach (var (slot, bindingId) in saveSlotBindingIds.ToArray())
        {
            if ((viewActive || Project.KeepRunning) && currentSlots.Contains(slot)) continue;

            globalHotkeys.SetBinding(bindingId, null, static _ => Task.CompletedTask);
            saveSlotBindingIds.Remove(slot);
        }

        if (!viewActive && !Project.KeepRunning) return;

        foreach (var slot in slots)
        {
            if (!saveSlotBindingIds.TryGetValue(slot, out var bindingId))
            {
                bindingId = $"{save_slot_binding_prefix}-{Guid.NewGuid():N}";
                saveSlotBindingIds.Add(slot, bindingId);
            }

            globalHotkeys.SetBinding(
                bindingId,
                ToGlobalHotkey(slot.ProjectHotkey),
                cancellationToken => LoadSaveSlotFromHotkeyAsync(slot, cancellationToken));
        }
    }

    private Task LoadSaveSlotFromHotkeyAsync(
        GeometryDashboardSaveSlot slot,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (disposed || !viewActive && !Project.KeepRunning) return Task.CompletedTask;

        lock (Project)
        {
            if (!Project.SaveSlots.Contains(slot)) return Task.CompletedTask;
        }

        LoadSaveSlot(slot);
        return Task.CompletedTask;
    }

    private static HotkeySettings? ToGlobalHotkey(HotkeySettings? hotkey)
    {
        return hotkey is null || hotkey.Key == 0 ? null : hotkey;
    }

    private void RefreshSaveSlotHotkeys()
    {
        SynchronizeSaveSlotHotkeys();
    }

    private void RebuildGroups()
    {
        var order = Enum.GetValues<GeneratorType>();
        GeneratorGroups.Clear();
        foreach (var type in order)
        {
            var generators = Generators
                .Where(generator => generator.Model.GeneratorType == type
                                    && (string.IsNullOrWhiteSpace(Filter)
                                        || generator.Name.Contains(Filter, StringComparison.OrdinalIgnoreCase)))
                .ToArray();
            if (generators.Length > 0)
                GeneratorGroups.Add(new GeometryDashboardGeneratorGroupViewModel(type.ToString(), generators));
        }
    }

    private static GeometryDashboardTargetingMode ToTargetingMode(KeyModifiers modifiers)
    {
        if (modifiers.HasFlag(KeyModifiers.Control)) return GeometryDashboardTargetingMode.Disable;
        if (modifiers.HasFlag(KeyModifiers.Shift)) return GeometryDashboardTargetingMode.Enable;
        return GeometryDashboardTargetingMode.Toggle;
    }
}
