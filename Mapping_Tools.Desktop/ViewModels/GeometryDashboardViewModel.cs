using System.Collections.ObjectModel;
using System.Globalization;
using Avalonia.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.GeometryDashboard;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Projects;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.SnappingTools;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectCollection;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorCollection;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;
using Mapping_Tools.Core.Tools.SnappingTools.Serialization;
using Mapping_Tools.Desktop.Shell;
using Material.Icons;

namespace Mapping_Tools.Desktop.ViewModels;

/// <summary>
///     Coordinates the Geometry Dashboard UI, Core graph, project persistence, and
///     the step-44 platform adapters. The view model owns no native handles.
/// </summary>
public sealed partial class GeometryDashboardViewModel : ObservableObject,
    IShellProjectFeature, IShellExtraProjectMenuFeature, IShellFeatureActivation, IDisposable
{
    private const double relevancy_bias = 4;
    private const double points_bias = 3;
    private const double special_bias = 2;
    private const double selection_range = 80;
    private static readonly HitObjectComparer hitObjectComparer = new();
    private readonly HashSet<SnappingToolsSaveSlot> activeSaveSlots = [];

    private readonly ApplicationSettings applicationSettings;
    private readonly CoordinateConverter converter = new();

    private readonly ProjectDefinition<SnappingToolsProject> definition =
        GeometryDashboardProjectDefinition.Definition;

    private readonly IGeometryDashboardDialogService dialogs;
    private readonly IUiDispatcher dispatcher;
    private readonly IFilePicker filePicker;
    private readonly ITextFileStore files;
    private readonly List<IRelevantDrawable> inheritableDrawables = [];
    private readonly IGeometryDashboardInputService input;
    private readonly LayerCollection layers;
    private readonly CancellationTokenSource lifetime = new();
    private readonly List<IRelevantDrawable> lockedDrawables = [];
    private readonly IUserNotificationService notifications;
    private readonly IGeometryDashboardOverlayHostFactory overlayFactory;
    private readonly IGeometryDashboardRuntime runtime;
    private readonly List<IRelevantDrawable> selectedDrawables = [];
    private readonly IProjectSerializer serializer;
    private readonly object stateGate = new();
    private bool active;
    private string? configurationStatus;
    private bool disposed;
    private string filter = string.Empty;
    private RelevantHitObject? heldHitObject;
    private IRelevantObject[] heldHitObjects = [];
    private Vector2 heldMouseOffset;
    private IRelevantDrawable? lastSnapped;
    private bool lockedToggle;
    private Task? loop;
    private IGeometryDashboardOverlayHost? overlay;
    private int readerFailures;
    private GeometryDashboardRuntimeSnapshot? runtimeSnapshot;
    private bool unlockedSomething;

    /// <summary>Creates the dashboard presentation and its default generator catalog.</summary>
    /// <param name="applicationSettings">Shared process settings.</param>
    /// <param name="runtime">Reads the external osu!/editor snapshot.</param>
    /// <param name="input">Reads global keyboard, mouse, and cursor state.</param>
    /// <param name="overlayFactory">Creates the click-through geometry overlay.</param>
    /// <param name="serializer">Reads and writes legacy-compatible JSON.</param>
    /// <param name="filePicker">Presents locked-object import/export pickers.</param>
    /// <param name="files">Reads the osu! configuration file.</param>
    /// <param name="notifications">Publishes user-visible operation outcomes.</param>
    /// <param name="dialogs">Presents the three dashboard-owned dialogs.</param>
    /// <param name="dispatcher">Marshals observable state changes to Avalonia's UI thread.</param>
    public GeometryDashboardViewModel(
        ApplicationSettings applicationSettings,
        IGeometryDashboardRuntime runtime,
        IGeometryDashboardInputService input,
        IGeometryDashboardOverlayHostFactory overlayFactory,
        IProjectSerializer serializer,
        IFilePicker filePicker,
        ITextFileStore files,
        IUserNotificationService notifications,
        IGeometryDashboardDialogService dialogs,
        IUiDispatcher dispatcher)
    {
        this.applicationSettings = applicationSettings ?? throw new ArgumentNullException(nameof(applicationSettings));
        this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        this.input = input ?? throw new ArgumentNullException(nameof(input));
        this.overlayFactory = overlayFactory ?? throw new ArgumentNullException(nameof(overlayFactory));
        this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        this.filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        this.files = files ?? throw new ArgumentNullException(nameof(files));
        this.notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        this.dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));

        Project = new SnappingToolsProject();
        Generators = new ObservableCollection<GeometryDashboardGeneratorViewModel>(CreateGenerators());
        Project.SetGenerators(Generators.Select(generator => generator.Model));
        layers = CreateLayers();
        RebuildGroups();
    }

    /// <summary>Gets the serializable project currently edited by the dashboard.</summary>
    public SnappingToolsProject Project { get; }

    /// <summary>Gets generator rows in the legacy reflection order.</summary>
    public ObservableCollection<GeometryDashboardGeneratorViewModel> Generators { get; }

    /// <summary>Gets filtered generator groups for the dashboard list.</summary>
    public ObservableCollection<GeometryDashboardGeneratorGroupViewModel> GeneratorGroups { get; } = [];

    /// <summary>Gets the current progress value used by the dashboard footer.</summary>
    [ObservableProperty]
    public partial double Progress { get; private set; }

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

    /// <summary>Gets the active preferences object edited by the dashboard.</summary>
    public SnappingToolsPreferences Preferences => Project.CurrentPreferences;

    /// <summary>Gets the generated geometry count displayed in diagnostics.</summary>
    public int DrawableCount => layers.GetAllRelevantDrawables().Count();

    /// <summary>Gets or sets the number of selected virtual objects.</summary>
    public int SelectedCount => layers.GetAllRelevantObjects().Count(objectModel => objectModel.IsSelected);

    /// <summary>Gets whether the platform and editor state are currently active.</summary>
    public bool IsConnected => runtimeSnapshot is not null && overlay?.IsVisible == true;

    /// <summary>Gets whether the current feature should keep its background loop alive.</summary>
    public bool KeepRunning => Preferences.KeepRunning;

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        active = false;
        lifetime.Cancel();
        try { loop?.Wait(TimeSpan.FromSeconds(1)); }
        catch { }

        overlay?.Dispose();
        lifetime.Dispose();
        lock (stateGate)
        {
            foreach (var objectModel in layers.GetAllRelevantObjects().ToArray()) objectModel.Dispose();
        }

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
        active = true;
        loop ??= Task.Run(() => RunLoopAsync(lifetime.Token));
    }

    /// <inheritdoc />
    public void Deactivate()
    {
        if (Preferences.KeepRunning) return;
        active = false;
        overlay?.Disable();
    }

    IProjectDefinition IShellProjectFeature.ProjectDefinition => definition;

    object IShellProjectFeature.Snapshot()
    {
        lock (stateGate)
        {
            lock (Project)
            {
                return Project.GetThis();
            }
        }
    }

    void IShellProjectFeature.Install(object project)
    {
        if (project is not SnappingToolsProject loaded)
            throw new InvalidDataException("The Geometry Dashboard project is incomplete.");

        lock (stateGate)
        {
            lock (Project)
            {
                Project.SaveSlots.Clear();
                foreach (var slot in loaded.SaveSlots) Project.SaveSlots.Add(slot);
                Project.SetCurrentPreferences(loaded.CurrentPreferences);
            }

            activeSaveSlots.Clear();
            ApplyPreferences();
        }
    }

    /// <summary>Runs one external-state update. This is public for focused UI behavior tests.</summary>
    /// <param name="cancellationToken">Cancels the adapter read and calculation.</param>
    /// <returns>A task that completes after the state has been reconciled.</returns>
    public Task RefreshOnceAsync(CancellationToken cancellationToken = default)
    {
        return RefreshOnceCoreAsync(cancellationToken);
    }

    /// <summary>Executes the selection toggle using the legacy Shift/Ctrl modifiers.</summary>
    /// <param name="modifiers">The modifiers captured from the dashboard button press.</param>
    public void ToggleSelected(KeyModifiers modifiers = KeyModifiers.None)
    {
        ToggleObjects(
            layers.GetAllRelevantDrawables(),
            (modifiers & KeyModifiers.Shift) != 0,
            (modifiers & KeyModifiers.Control) != 0,
            static objectModel => objectModel.IsSelected,
            static (objectModel, value) => objectModel.IsSelected = value);
    }

    /// <summary>Executes the lock toggle using the legacy Shift/Ctrl modifiers.</summary>
    /// <param name="modifiers">The modifiers captured from the dashboard button press.</param>
    public void ToggleLocked(KeyModifiers modifiers = KeyModifiers.None)
    {
        ToggleLockedObjects(
            (modifiers & KeyModifiers.Shift) != 0,
            (modifiers & KeyModifiers.Control) != 0);
    }

    /// <summary>Executes the inheritable toggle using the legacy Shift/Ctrl modifiers.</summary>
    /// <param name="modifiers">The modifiers captured from the dashboard button press.</param>
    public void ToggleInheritable(KeyModifiers modifiers = KeyModifiers.None)
    {
        ToggleObjects(
            layers.GetAllRelevantDrawables(),
            (modifiers & KeyModifiers.Shift) != 0,
            (modifiers & KeyModifiers.Control) != 0,
            static objectModel => objectModel.IsInheritable,
            static (objectModel, value) => objectModel.IsInheritable = value);
    }

    /// <summary>Shows the preferences dialog and applies an accepted clone.</summary>
    public async Task ShowPreferencesAsync()
    {
        var preferences = await dialogs.ShowPreferencesAsync(
            (SnappingToolsPreferences)Preferences.Clone());
        if (preferences is not null)
        {
            Project.SetCurrentPreferences(preferences);
            ApplyPreferences();
        }
    }

    /// <summary>Shows the modeless save-slot dialog for the current project.</summary>
    public Task ShowProjectSlotsAsync()
    {
        return dialogs.ShowProjectSlotsAsync(Project, LoadSaveSlot, RefreshSaveSlotHotkeys);
    }

    /// <summary>Shows a generator's typed settings dialog and regenerates after acceptance.</summary>
    /// <param name="generator">The generator row requesting configuration.</param>
    public async Task ShowGeneratorSettingsAsync(GeometryDashboardGeneratorViewModel generator)
    {
        ArgumentNullException.ThrowIfNull(generator);
        if (await dialogs.ShowGeneratorSettingsAsync(generator.Model.Settings)) Regenerate();
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

            string json = serializer.Serialize(GetLockedObjects());
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
                string.Join(Environment.NewLine, files.ReadAllLines(paths[0])));
            SetLockedObjects(objects);
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

    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (active || Preferences.KeepRunning)
                try { await RefreshOnceCoreAsync(cancellationToken).ConfigureAwait(false); }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { break; }
                catch (Exception exception)
                {
                    readerFailures++;
                    SetStatus(readerFailures >= 3
                        ? "Editor Reader seems to be failing a lot..."
                        : exception.Message);
                    overlay?.Disable();
                }

            try { await Task.Delay(runtimeSnapshot is null ? 1000 : 100, cancellationToken).ConfigureAwait(false); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task RefreshOnceCoreAsync(CancellationToken cancellationToken)
    {
        if (!input.IsSupported)
        {
            SetStatus("Geometry Dashboard requires Windows.");
            overlay?.Disable();
            return;
        }

        if (!applicationSettings.UseEditorReader)
        {
            SetStatus("Enable Editor Reader in Preferences to use Geometry Dashboard.");
            overlay?.Disable();
            return;
        }

        var snapshot = await runtime.ReadAsync(cancellationToken);
        if (snapshot is null)
        {
            SetStatus("Waiting for an open editor...");
            runtimeSnapshot = null;
            overlay?.Disable();
            return;
        }

        var previousSnapshot = runtimeSnapshot;
        bool shouldUpdateRoots = previousSnapshot is null
                                 || Preferences.UpdateMode switch
                                 {
                                     UpdateMode.AnyChange => true,
                                     UpdateMode.TimeChange => previousSnapshot.Editor.EditorTime != snapshot.Editor.EditorTime,
                                     UpdateMode.OsuActivated => snapshot.Window.IsActivated && !previousSnapshot.Window.IsActivated,
                                     UpdateMode.HotkeyDown => false,
                                     _ => true,
                                 };
        runtimeSnapshot = snapshot;
        readerFailures = 0;
        UpdateConverter(snapshot);
        if (shouldUpdateRoots || input.IsHotkeyDown(Preferences.RefreshHotkey)) UpdateRootObjects(snapshot.Editor);
        if (!snapshot.Window.IsActivated)
        {
            UpdateOverlay(snapshot);
            SetStatus("Waiting for osu! to become active...");
            return;
        }

        UpdateHotkeys();
        UpdateOverlay(snapshot);
        SetStatus(configurationStatus
                  ?? (layers.GetAllRelevantObjects().Any()
                      ? $"{DrawableCount} virtual object(s)"
                      : "No visible hit objects."));
        NotifyPropertyChanged(nameof(DrawableCount));
        NotifyPropertyChanged(nameof(SelectedCount));
    }

    private void UpdateConverter(GeometryDashboardRuntimeSnapshot snapshot)
    {
        configurationStatus = null;
        converter.OsuWindowPosition = new Vector2(snapshot.Window.Bounds.Left, snapshot.Window.Bounds.Top);
        converter.ScreenBox = snapshot.PrimaryScreen?.Bounds ?? snapshot.Window.Bounds;
        converter.DpiMultiplier = snapshot.Window.DpiScale;
        converter.DpiSourceAvailable = snapshot.Window.DpiSourceAvailable;

        if (!string.IsNullOrWhiteSpace(applicationSettings.OsuConfigPath))
            try
            {
                var values = ReadConfig(applicationSettings.OsuConfigPath);
                converter.Fullscreen = GetBool(values, "Fullscreen", true);
                converter.Letterboxing = GetBool(values, "Letterboxing", true);
                converter.OsuResolution = new Vector2(
                    GetDouble(values, converter.Fullscreen ? "WidthFullscreen" : "Width", converter.OsuResolution.X),
                    GetDouble(values, converter.Fullscreen ? "HeightFullscreen" : "Height", converter.OsuResolution.Y));
                converter.LetterboxingPosition = new Vector2(
                    GetDouble(values, "LetterboxPositionX", converter.LetterboxingPosition.X),
                    GetDouble(values, "LetterboxPositionY", converter.LetterboxingPosition.Y));
            }
            catch (Exception exception)
            {
                configurationStatus = "Could not read osu! configuration: " + exception.Message;
            }
        else
            configurationStatus = "Specify your osu! user configuration file in Mapping Tools Preferences.";

        converter.EditorBoxOffset = Preferences.OverlayOffset;
        layers.AcceptableDifference = Preferences.AcceptableDifference;
        layers.SetInceptionLevel(Preferences.InceptionLevel);
    }

    private bool UpdateRootObjects(GeometryDashboardEditorSnapshot editor)
    {
        lock (stateGate)
        {
            double approachTime = Beatmap.GetApproachTime(editor.ApproachRate);
            var candidates = Preferences.SelectedHitObjectMode switch
            {
                SelectedHitObjectMode.OnlySelected => editor.SelectedHitObjects,
                SelectedHitObjectMode.VisibleOrSelected when editor.SelectedHitObjects.Count > 0 =>
                    editor.SelectedHitObjects,
                _ => editor.HitObjects.Where(objectModel => editor.EditorTime > objectModel.Time - approachTime && editor.EditorTime < objectModel.EndTime + approachTime),
            };
            var objects = candidates.ToArray();
            var existing = layers.GetRootRelevantHitObjects().ToArray();
            var removed = existing
                .Where(old => !objects.Any(candidate => SameHitObject(old.HitObject, candidate)))
                .ToArray();
            var added = objects
                .Where(candidate => !existing.Any(old => SameHitObject(old.HitObject, candidate)))
                .ToArray();
            foreach (var oldObject in removed)
                oldObject.Dispose();

            layers.GetRootLayer().Add(added.Select(candidate => new RelevantHitObject(candidate)));
            bool selectionChanged = SynchronizeRootSelection(
                layers.GetRootRelevantHitObjects(),
                editor.SelectedHitObjects);
            if (added.Length == 0 && removed.Length == 0 && !selectionChanged) return false;

            layers.GetRootLayer().GenerateNewObjects(true);
            return true;
        }
    }

    private static bool SynchronizeRootSelection(
        IEnumerable<RelevantHitObject> roots,
        IReadOnlyList<HitObject> selectedHitObjects)
    {
        bool changed = false;
        foreach (var root in roots)
        {
            bool isSelected = selectedHitObjects.Any(selectedHitObject =>
                hitObjectComparer.Equals(root.HitObject, selectedHitObject));
            if (root.IsSelected == isSelected) continue;

            bool autoPropagate = root.AutoPropagate;
            root.AutoPropagate = false;
            root.IsSelected = isSelected;
            root.AutoPropagate = autoPropagate;
            changed = true;
        }

        return changed;
    }

    private void UpdateHotkeys()
    {
        lock (stateGate)
        {
            Vector2 screen;
            lock (Project)
            {
                foreach (var slot in Project.SaveSlots.ToArray())
                {
                    bool isDown = input.IsHotkeyDown(slot.ProjectHotkey);
                    if (isDown && activeSaveSlots.Add(slot)) LoadSaveSlot(slot);
                    if (!isDown) activeSaveSlots.Remove(slot);
                }
            }

            if (input.IsMouseButtonDown(GeometryDashboardMouseButton.Left) && input.TryGetCursorPosition(out screen))
            {
                var cursor = converter.ScreenToEditorCoordinate(screen);
                var selected = layers.GetRootRelevantHitObjects().Where(objectModel => objectModel.IsSelected).ToArray();
                heldHitObjects = selected;
                heldHitObject = selected.OrderBy(objectModel => Vector2.Distance(objectModel.HitObject.Pos, cursor))
                    .FirstOrDefault(objectModel => Vector2.Distance(objectModel.HitObject.Pos, cursor) <= Beatmap.GetHitObjectRadius(runtimeSnapshot?.Editor.CircleSize ?? 5));
                heldMouseOffset = heldHitObject is null ? Vector2.Zero : heldHitObject.HitObject.Pos - cursor;
            }
            else
            {
                heldHitObject = null;
                heldHitObjects = [];
                heldMouseOffset = Vector2.Zero;
            }

            bool snap = input.IsHotkeyDown(Preferences.SnapHotkey);
            if (!snap) lastSnapped = null;
            if (snap && input.TryGetCursorPosition(out screen))
            {
                var cursor = converter.ScreenToEditorCoordinate(screen);
                var nearest = GetNearestDrawable(
                    cursor + heldMouseOffset,
                    heldObjects: heldHitObjects,
                    specialPriority: static objectModel =>
                        objectModel.IsSelected || objectModel.IsLocked || objectModel.IsInheritable);
                if (nearest is not null)
                {
                    lastSnapped = nearest;
                    input.TrySetCursorPosition(converter.EditorToScreenCoordinate(
                        nearest.NearestPoint(cursor + heldMouseOffset) - heldMouseOffset));
                }
            }

            if (input.IsHotkeyDown(Preferences.SelectHotkey))
                ApplyNearestToggle(selectedDrawables, static objectModel => objectModel.IsSelected, static (objectModel, value) => objectModel.IsSelected = value);
            else selectedDrawables.Clear();
            if (input.IsHotkeyDown(Preferences.LockHotkey))
            {
                ApplyNearestLock();
            }
            else
            {
                lockedDrawables.Clear();
                unlockedSomething = false;
            }

            if (input.IsHotkeyDown(Preferences.InheritHotkey))
                ApplyNearestToggle(inheritableDrawables, static objectModel => objectModel.IsInheritable, static (objectModel, value) => objectModel.IsInheritable = value);
            else inheritableDrawables.Clear();
        }
    }

    private void UpdateOverlay(GeometryDashboardRuntimeSnapshot snapshot)
    {
        overlay ??= overlayFactory.Create();
        if (!overlay.IsSupported) return;
        if (overlay.TargetWindow != snapshot.Window.Id) overlay.Initialize(snapshot.Window.Id);
        overlay.Enable();
        var editorBox = converter.GetEditorBox();
        var frame = BuildFrame();
        overlay.SetFrame(frame);
        overlay.SetBorder(Preferences.DebugEnabled);
        overlay.Update(editorBox, snapshot.Window.DpiScale, snapshot.Window.DpiSourceAvailable);
    }

    private GeometryDashboardOverlayFrame BuildFrame()
    {
        lock (stateGate)
        {
            if (runtimeSnapshot is null) return GeometryDashboardOverlayFrame.Empty;
            var drawables = layers.GetAllRelevantDrawables();
            if (input.IsHotkeyDown(Preferences.SnapHotkey))
            {
                var viewMode = Preferences.KeyDownViewMode;
                if (!viewMode.HasFlag(ViewMode.Everything))
                {
                    List<IRelevantDrawable> related = [];
                    if (lastSnapped is not null)
                    {
                        if (viewMode.HasFlag(ViewMode.Parents))
                            related.AddRange(lastSnapped.GetParentage(int.MaxValue).OfType<IRelevantDrawable>());
                        else if (viewMode.HasFlag(ViewMode.DirectParents)) related.AddRange(lastSnapped.GetParentage(1).OfType<IRelevantDrawable>());

                        if (viewMode.HasFlag(ViewMode.Children))
                            related.AddRange(lastSnapped.GetDescendants(int.MaxValue).OfType<IRelevantDrawable>());
                        else if (viewMode.HasFlag(ViewMode.DirectChildren)) related.AddRange(lastSnapped.GetDescendants(1).OfType<IRelevantDrawable>());
                    }

                    drawables = related;
                }
            }
            else if (!Preferences.KeyUpViewMode.HasFlag(ViewMode.Everything))
            {
                drawables = [];
            }

            List<GeometryDashboardOverlayShape> shapes = [];
            if (Preferences.VisiblePlayfieldBoundary)
            {
                Vector2[] boundary =
                [
                    new(-65, -57), new(576, -57), new(576, 423), new(-65, 423), new(-65, -57),
                ];
                for (int index = 0; index < boundary.Length - 1; index++)
                    shapes.Add(new GeometryDashboardOverlayShape(
                        GeometryDashboardOverlayShapeKind.Line,
                        ToOverlayPoint(boundary[index]),
                        ToOverlayPoint(boundary[index + 1]),
                        0,
                        RgbaColour.FromRgb(255, 140, 0),
                        2,
                        1,
                        DashStylesEnum.Solid));
            }

            foreach (var drawable in drawables.OfType<IRelevantDrawable>().Distinct())
            {
                var preferences = Preferences.GetReleventObjectPreferences(drawable.PreferencesName);
                if (drawable.IsSelected) AddDrawableShape(shapes, drawable, preferences, true);

                AddDrawableShape(shapes, drawable, preferences, false);
            }

            return new GeometryDashboardOverlayFrame(shapes);
        }
    }

    private void AddDrawableShape(
        List<GeometryDashboardOverlayShape> shapes,
        IRelevantDrawable drawable,
        RelevantObjectPreferences preferences,
        bool selectedPass)
    {
        var colour = selectedPass
            ? RgbaColour.FromRgb(255, 200, 0)
            : AdjustColour(
                preferences.Color,
                drawable.IsLocked ? drawable.IsSelected ? 0.6 : 0.3 : 1,
                drawable.IsInheritable ? 1 : 0.5);
        double opacity = drawable.Relevancy * preferences.Opacity;
        double thickness = preferences.Thickness + (selectedPass ? 2 : 0);

        switch (drawable)
        {
            case RelevantPoint point:
                shapes.Add(new GeometryDashboardOverlayShape(
                    GeometryDashboardOverlayShapeKind.Point,
                    ToOverlayPoint(point.Child),
                    default,
                    preferences.Size,
                    colour,
                    opacity,
                    thickness,
                    preferences.Dashstyle));
                break;
            case RelevantCircle circle:
                shapes.Add(new GeometryDashboardOverlayShape(
                    GeometryDashboardOverlayShapeKind.Circle,
                    ToOverlayPoint(circle.Child.Centre),
                    default,
                    converter.ToDpi(converter.ScaleByRatio(new Vector2(circle.Child.Radius, 0))).X,
                    colour,
                    opacity,
                    thickness,
                    preferences.Dashstyle));
                break;
            case RelevantLine line when Line2.Intersection(
                                            new Box2(-1000, -1000, 1512, 1384), line.Child, out var intersections)
                                        && intersections.Length >= 2:
                shapes.Add(new GeometryDashboardOverlayShape(
                    GeometryDashboardOverlayShapeKind.Line,
                    ToOverlayPoint(intersections[0]),
                    ToOverlayPoint(intersections[1]),
                    0,
                    colour,
                    opacity,
                    thickness,
                    preferences.Dashstyle));
                break;
        }
    }

    private Vector2 ToOverlayPoint(Vector2 editorCoordinate)
    {
        var editorBox = converter.GetEditorBox();
        var relative = converter.EditorToRelativeCoordinate(editorCoordinate);
        return new Vector2(editorBox.Left + relative.X, editorBox.Top + relative.Y);
    }

    private void ApplyNearestToggle(List<IRelevantDrawable> handled, Func<IRelevantDrawable, bool> read, Action<IRelevantDrawable, bool> write)
    {
        if (!input.TryGetCursorPosition(out var screen)) return;
        var nearest = GetNearestDrawable(
            converter.ScreenToEditorCoordinate(screen),
            selection_range,
            specialPriority: read);
        if (nearest is null || handled.Contains(nearest)) return;
        bool value = handled.Count == 0 ? !read(nearest) : read(handled[0]);
        nearest.AutoPropagate = false;
        write(nearest, value);
        nearest.AutoPropagate = true;
        handled.Add(nearest);
        Regenerate();
    }

    private void ApplyNearestLock()
    {
        if (!input.TryGetCursorPosition(out var screen)) return;
        var nearest = GetNearestDrawable(
            converter.ScreenToEditorCoordinate(screen),
            selection_range,
            specialPriority: static objectModel => objectModel.IsLocked);
        if (nearest is null || lockedDrawables.Contains(nearest)) return;
        if (lockedDrawables.Count == 0) lockedToggle = !nearest.IsLocked;
        if (lockedToggle)
        {
            layers.GetRootLayer().Add(nearest.GetLockedRelevantObject());
        }
        else if (nearest.IsLocked && !unlockedSomething)
        {
            nearest.Dispose();
            unlockedSomething = true;
        }

        lockedDrawables.Add(nearest);
        Regenerate();
    }

    private IRelevantDrawable? GetNearestDrawable(
        Vector2 cursor,
        double range = double.PositiveInfinity,
        IRelevantObject[]? heldObjects = null,
        Func<IRelevantDrawable, bool>? specialPriority = null)
    {
        lock (stateGate)
        {
            IRelevantDrawable? nearest = null;
            double best = double.PositiveInfinity;
            foreach (var drawable in layers.GetAllRelevantDrawables())
            {
                if (heldObjects is not null
                    && drawable.ParentObjects.Count > 0
                    && drawable.ParentObjects.All(parent => parent is RelevantHitObject hit && heldObjects.Contains(hit))) continue;
                double distance = drawable.DistanceTo(cursor);
                if (distance > range) continue;
                distance -= relevancy_bias * Math.Clamp(drawable.Relevancy, 0, 1);
                if (drawable is RelevantPoint) distance -= points_bias;
                if (specialPriority?.Invoke(drawable) == true) distance -= special_bias;
                if (distance < best)
                {
                    best = distance;
                    nearest = drawable;
                }
            }

            return nearest;
        }
    }

    private void ToggleObjects(
        IEnumerable<IRelevantDrawable> objects,
        bool enable,
        bool disable,
        Func<IRelevantDrawable, bool> read,
        Action<IRelevantDrawable, bool> write)
    {
        lock (stateGate)
        {
            var values = objects.ToArray();
            bool target = enable || !disable && values.Any(value => !read(value));
            if (disable) target = false;
            foreach (var value in values)
            {
                value.AutoPropagate = false;
                write(value, target);
                value.AutoPropagate = true;
            }

            Regenerate();
        }
    }

    private void ToggleLockedObjects(bool enable, bool disable)
    {
        lock (stateGate)
        {
            var values = layers.GetAllRelevantDrawables().ToArray();
            bool target = enable || !disable && values.Any(value => !value.IsLocked);
            if (disable) target = false;

            if (target)
                foreach (var value in values.Where(value => !value.IsLocked))
                    layers.GetRootLayer().Add(value.GetLockedRelevantObject());
            else
                foreach (var value in values.Where(value => value.IsLocked))
                    value.Dispose();

            Regenerate();
        }
    }

    private void Regenerate()
    {
        lock (stateGate)
        {
            layers.GetRootLayer().GenerateNewObjects(true);
            overlay?.SetFrame(BuildFrame());
            overlay?.Invalidate();
            NotifyPropertyChanged(nameof(DrawableCount));
            NotifyPropertyChanged(nameof(SelectedCount));
        }
    }

    private void ApplyPreferences()
    {
        lock (stateGate)
        {
            converter.EditorBoxOffset = Preferences.OverlayOffset;
            layers.AcceptableDifference = Preferences.AcceptableDifference;
            layers.SetInceptionLevel(Preferences.InceptionLevel);
            Regenerate();
        }
    }

    private void SetStatus(string value)
    {
        dispatcher.Post(() =>
        {
            if (!disposed) Status = value;
        });
    }

    private void NotifyPropertyChanged(string propertyName)
    {
        dispatcher.Post(() =>
        {
            if (!disposed) OnPropertyChanged(propertyName);
        });
    }

    private void LoadSaveSlot(SnappingToolsSaveSlot slot)
    {
        dispatcher.Post(() =>
        {
            if (disposed) return;
            lock (stateGate)
            {
                lock (Project)
                {
                    Project.LoadFromSlot(slot);
                }

                ApplyPreferences();
            }
        });
    }

    private void RefreshSaveSlotHotkeys()
    {
        lock (stateGate)
        {
            activeSaveSlots.Clear();
        }
    }

    private RelevantObjectCollection GetLockedObjects()
    {
        lock (stateGate)
        {
            RelevantObjectCollection collection = new();
            foreach (var objectModel in layers.GetAllRelevantObjects().Where(objectModel => objectModel.IsLocked))
                collection.GetOrCreate(objectModel.GetType()).Add(objectModel.GetLockedRelevantObject());
            return collection;
        }
    }

    private void SetLockedObjects(RelevantObjectCollection objects)
    {
        lock (stateGate)
        {
            foreach (var values in objects.Values) layers.GetRootLayer().Add(values.Select(value => value.GetLockedRelevantObject()));
            Regenerate();
        }
    }

    private IEnumerable<GeometryDashboardGeneratorViewModel> CreateGenerators()
    {
        return typeof(RelevantObjectsGenerator).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(RelevantObjectsGenerator).IsAssignableFrom(type) && type.GetConstructor(Type.EmptyTypes) is not null)
            .Select(type => (RelevantObjectsGenerator)Activator.CreateInstance(type)!)
            .Select(generator => new GeometryDashboardGeneratorViewModel(generator, this));
    }

    private LayerCollection CreateLayers()
    {
        RelevantObjectsGeneratorCollection collection = new(Generators.Select(generator => generator.Model));
        return new LayerCollection(collection, Preferences.AcceptableDifference);
    }

    private void RebuildGroups()
    {
        var order = Enum.GetValues<GeneratorType>();
        GeneratorGroups.Clear();
        foreach (var type in order)
        {
            var generators = Generators
                .Where(generator => generator.Model.GeneratorType == type
                                    && (string.IsNullOrWhiteSpace(Filter) || generator.Name.Contains(Filter, StringComparison.OrdinalIgnoreCase)))
                .ToArray();
            if (generators.Length > 0) GeneratorGroups.Add(new GeometryDashboardGeneratorGroupViewModel(type.ToString(), generators));
        }
    }

    private static bool SameHitObject(HitObject first, HitObject second)
    {
        return hitObjectComparer.Equals(first, second);
    }

    private Dictionary<string, string> ReadConfig(string path)
    {
        return new Dictionary<string, string>(
            files.ReadAllLines(path)
                .Select(line => line.Split(['=', ':'], 2))
                .Where(parts => parts.Length == 2)
                .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim(), StringComparer.OrdinalIgnoreCase),
            StringComparer.OrdinalIgnoreCase);
    }

    private static bool GetBool(Dictionary<string, string> values, string key, bool fallback)
    {
        return values.TryGetValue(key, out string? value) ? value == "1" || bool.TryParse(value, out bool result) && result : fallback;
    }

    private static double GetDouble(Dictionary<string, string> values, string key, double fallback)
    {
        return values.TryGetValue(key, out string? value) && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result)
            ? result
            : fallback;
    }

    private static RgbaColour AdjustColour(RgbaColour colour, double saturationMultiplier, double brightnessMultiplier)
    {
        double max = Math.Max(colour.R, Math.Max(colour.G, colour.B)) / 255d;
        double min = Math.Min(colour.R, Math.Min(colour.G, colour.B)) / 255d;
        double brightness = max * brightnessMultiplier;
        double saturation = (max - min) * saturationMultiplier;
        if (max <= 0) return RgbaColour.FromArgb(colour.A, 0, 0, 0);
        double hue = GetHue(colour.R / 255d, colour.G / 255d, colour.B / 255d, max, min);
        return FromHsv(hue, max == 0 ? 0 : saturation / max, brightness, colour.A);
    }

    private static double GetHue(double red, double green, double blue, double max, double min)
    {
        if (max == min) return 0;
        double delta = max - min;
        if (max == red) return 60 * ((green - blue) / delta % 6);
        if (max == green) return 60 * ((blue - red) / delta + 2);
        return 60 * ((red - green) / delta + 4);
    }

    private static RgbaColour FromHsv(double hue, double saturation, double value, byte alpha)
    {
        double chroma = value * saturation;
        double x = chroma * (1 - Math.Abs(hue / 60 % 2 - 1));
        double m = value - chroma;
        (double r, double g, double b) values;
        if (hue < 60) values = (chroma, x, 0);
        else if (hue < 120) values = (x, chroma, 0);
        else if (hue < 180) values = (0, chroma, x);
        else if (hue < 240) values = (0, x, chroma);
        else if (hue < 300) values = (x, 0, chroma);
        else values = (chroma, 0, x);
        (double r, double g, double b) = values;
        return RgbaColour.FromArgb(alpha, (byte)Math.Clamp((r + m) * 255, 0, 255), (byte)Math.Clamp((g + m) * 255, 0, 255), (byte)Math.Clamp((b + m) * 255, 0, 255));
    }
}

/// <summary>Provides the window interactions owned by the Geometry Dashboard.</summary>
public interface IGeometryDashboardDialogService
{
    /// <summary>Shows preferences and returns an accepted clone, or null on cancel.</summary>
    Task<SnappingToolsPreferences?> ShowPreferencesAsync(SnappingToolsPreferences preferences);

    /// <summary>Shows the modeless save-slot editor.</summary>
    /// <param name="project">The project whose slots are edited.</param>
    /// <param name="loadSlot">Loads one slot into the active dashboard.</param>
    /// <param name="refreshHotkeys">Refreshes the active save-slot shortcut registrations.</param>
    Task ShowProjectSlotsAsync(
        SnappingToolsProject project,
        Action<SnappingToolsSaveSlot> loadSlot,
        Action refreshHotkeys);

    /// <summary>Shows generator-specific settings and returns whether Apply was pressed.</summary>
    Task<bool> ShowGeneratorSettingsAsync(GeneratorSettings settings);
}

/// <summary>Wraps one Core generator for compiled Avalonia bindings.</summary>
public sealed partial class GeometryDashboardGeneratorViewModel : ObservableObject
{
    private readonly GeometryDashboardViewModel owner;

    /// <summary>Creates a generator row.</summary>
    public GeometryDashboardGeneratorViewModel(RelevantObjectsGenerator model, GeometryDashboardViewModel owner)
    {
        Model = model ?? throw new ArgumentNullException(nameof(model));
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    /// <summary>Gets the Core generator.</summary>
    public RelevantObjectsGenerator Model { get; }

    /// <summary>Gets the display name.</summary>
    public string Name => Model.Name;

    /// <summary>Gets the tooltip text.</summary>
    public string Tooltip => Model.Tooltip;

    /// <summary>Gets the settings object shown in the row.</summary>
    public GeneratorSettings Settings => Model.Settings;

    /// <summary>Shows this generator's settings dialog.</summary>
    [RelayCommand]
    private Task OpenSettingsAsync()
    {
        return owner.ShowGeneratorSettingsAsync(this);
    }
}

/// <summary>Contains a filtered generator group.</summary>
public sealed class GeometryDashboardGeneratorGroupViewModel
{
    /// <summary>Creates a group with the retained legacy heading.</summary>
    public GeometryDashboardGeneratorGroupViewModel(string name, IEnumerable<GeometryDashboardGeneratorViewModel> generators)
    {
        Name = name;
        Generators = new ObservableCollection<GeometryDashboardGeneratorViewModel>(generators);
    }

    /// <summary>Gets the group heading.</summary>
    public string Name { get; }

    /// <summary>Gets the rows in this group.</summary>
    public ObservableCollection<GeometryDashboardGeneratorViewModel> Generators { get; }

    /// <summary>Gets the visible row count rendered in the heading.</summary>
    public int ItemCount => Generators.Count;
}

internal static class RelevantObjectCollectionExtensions
{
    public static List<IRelevantObject> GetOrCreate(this RelevantObjectCollection collection, Type type)
    {
        if (!collection.TryGetValue(type, out var values))
        {
            values = [];
            collection[type] = values;
        }

        return values;
    }
}
