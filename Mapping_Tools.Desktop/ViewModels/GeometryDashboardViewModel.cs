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
using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Classes.MathUtil;
using Mapping_Tools.Core.Classes.Tools.SnappingTools;
using Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure;
using Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObject.RelevantObjects;
using Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObjectCollection;
using Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators;
using Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorCollection;
using Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;
using Mapping_Tools.Core.Classes.Tools.SnappingTools.Serialization;
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
    private const double RelevancyBias = 4;
    private const double PointsBias = 3;
    private const double SpecialBias = 2;
    private const double SelectionRange = 80;
    private static readonly HitObjectComparer HitObjectComparer = new();
    private readonly HashSet<SnappingToolsSaveSlot> _activeSaveSlots = [];

    private readonly ApplicationSettings _applicationSettings;
    private readonly CoordinateConverter _converter = new();

    private readonly ProjectDefinition<SnappingToolsProject> _definition =
        GeometryDashboardProjectDefinition.Definition;

    private readonly IGeometryDashboardDialogService _dialogs;
    private readonly IUiDispatcher _dispatcher;
    private readonly IFilePicker _filePicker;
    private readonly ITextFileStore _files;
    private readonly List<IRelevantDrawable> _inheritableDrawables = [];
    private readonly IGeometryDashboardInputService _input;
    private readonly LayerCollection _layers;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly List<IRelevantDrawable> _lockedDrawables = [];
    private readonly IUserNotificationService _notifications;
    private readonly IGeometryDashboardOverlayHostFactory _overlayFactory;
    private readonly IGeometryDashboardRuntime _runtime;
    private readonly List<IRelevantDrawable> _selectedDrawables = [];
    private readonly IProjectSerializer _serializer;
    private readonly object _stateGate = new();
    private bool _active;
    private string? _configurationStatus;
    private bool _disposed;
    private string _filter = string.Empty;
    private RelevantHitObject? _heldHitObject;
    private IRelevantObject[] _heldHitObjects = [];
    private Vector2 _heldMouseOffset;
    private IRelevantDrawable? _lastSnapped;
    private bool _lockedToggle;
    private Task? _loop;
    private IGeometryDashboardOverlayHost? _overlay;
    private int _readerFailures;
    private GeometryDashboardRuntimeSnapshot? _runtimeSnapshot;
    private bool _unlockedSomething;

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
        _applicationSettings = applicationSettings ?? throw new ArgumentNullException(nameof(applicationSettings));
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _input = input ?? throw new ArgumentNullException(nameof(input));
        _overlayFactory = overlayFactory ?? throw new ArgumentNullException(nameof(overlayFactory));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        _files = files ?? throw new ArgumentNullException(nameof(files));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));

        Project = new SnappingToolsProject();
        Generators = new ObservableCollection<GeometryDashboardGeneratorViewModel>(CreateGenerators());
        Project.SetGenerators(Generators.Select(generator => generator.Model));
        _layers = CreateLayers();
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
        get => _filter;
        set
        {
            string normalized = value ?? string.Empty;
            if (SetProperty(ref _filter, normalized)) RebuildGroups();
        }
    }

    /// <summary>Gets the active preferences object edited by the dashboard.</summary>
    public SnappingToolsPreferences Preferences => Project.CurrentPreferences;

    /// <summary>Gets the generated geometry count displayed in diagnostics.</summary>
    public int DrawableCount => _layers.GetAllRelevantDrawables().Count();

    /// <summary>Gets or sets the number of selected virtual objects.</summary>
    public int SelectedCount => _layers.GetAllRelevantObjects().Count(objectModel => objectModel.IsSelected);

    /// <summary>Gets whether the platform and editor state are currently active.</summary>
    public bool IsConnected => _runtimeSnapshot is not null && _overlay?.IsVisible == true;

    /// <summary>Gets whether the current feature should keep its background loop alive.</summary>
    public bool KeepRunning => Preferences.KeepRunning;

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _active = false;
        _lifetime.Cancel();
        try { _loop?.Wait(TimeSpan.FromSeconds(1)); }
        catch { }

        _overlay?.Dispose();
        _lifetime.Dispose();
        lock (_stateGate)
        {
            foreach (var objectModel in _layers.GetAllRelevantObjects().ToArray()) objectModel.Dispose();
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
        if (_disposed) return;
        _active = true;
        _loop ??= Task.Run(() => RunLoopAsync(_lifetime.Token));
    }

    /// <inheritdoc />
    public void Deactivate()
    {
        if (Preferences.KeepRunning) return;
        _active = false;
        _overlay?.Disable();
    }

    IProjectDefinition IShellProjectFeature.ProjectDefinition => _definition;

    object IShellProjectFeature.Snapshot()
    {
        lock (_stateGate)
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

        lock (_stateGate)
        {
            lock (Project)
            {
                Project.SaveSlots.Clear();
                foreach (var slot in loaded.SaveSlots) Project.SaveSlots.Add(slot);
                Project.SetCurrentPreferences(loaded.CurrentPreferences);
            }

            _activeSaveSlots.Clear();
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
            _layers.GetAllRelevantDrawables(),
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
            _layers.GetAllRelevantDrawables(),
            (modifiers & KeyModifiers.Shift) != 0,
            (modifiers & KeyModifiers.Control) != 0,
            static objectModel => objectModel.IsInheritable,
            static (objectModel, value) => objectModel.IsInheritable = value);
    }

    /// <summary>Shows the preferences dialog and applies an accepted clone.</summary>
    public async Task ShowPreferencesAsync()
    {
        var preferences = await _dialogs.ShowPreferencesAsync(
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
        return _dialogs.ShowProjectSlotsAsync(Project, LoadSaveSlot, RefreshSaveSlotHotkeys);
    }

    /// <summary>Shows a generator's typed settings dialog and regenerates after acceptance.</summary>
    /// <param name="generator">The generator row requesting configuration.</param>
    public async Task ShowGeneratorSettingsAsync(GeometryDashboardGeneratorViewModel generator)
    {
        ArgumentNullException.ThrowIfNull(generator);
        if (await _dialogs.ShowGeneratorSettingsAsync(generator.Model.Settings)) Regenerate();
    }

    /// <summary>Exports detached locked virtual objects using a native save picker.</summary>
    [RelayCommand]
    private async Task SaveLockedObjectsAsync()
    {
        try
        {
            string? path = await _filePicker.PickSaveFileAsync(new SaveFilePickerRequest
            {
                Title = "Save locked virtual objects",
                SuggestedFileName = "locked-virtual-objects.json",
                DefaultExtension = ".json",
                Filters = [CommonFilePickerFilters.MappingToolsProjects],
            });
            if (string.IsNullOrWhiteSpace(path)) return;

            string json = _serializer.Serialize(GetLockedObjects());
            _files.WriteAllLines(path, json.Split(["\r\n", "\n"], StringSplitOptions.None));
            await _notifications.PublishAsync(new UserNotification(
                UserNotificationSeverity.Success,
                "Save virtual objects",
                "Successfully saved locked virtual objects!"));
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            await _notifications.PublishAsync(new UserNotification(
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
            var paths = await _filePicker.PickOpenFilesAsync(new OpenFilePickerRequest
            {
                Title = "Load locked virtual objects",
                AllowMultiple = false,
                Filters = [CommonFilePickerFilters.MappingToolsProjects],
            });
            if (paths.Count == 0) return;

            var objects = _serializer.Deserialize<RelevantObjectCollection>(
                string.Join(Environment.NewLine, _files.ReadAllLines(paths[0])));
            SetLockedObjects(objects);
            await _notifications.PublishAsync(new UserNotification(
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
            await _notifications.PublishAsync(new UserNotification(
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
            if (_active || Preferences.KeepRunning)
                try { await RefreshOnceCoreAsync(cancellationToken).ConfigureAwait(false); }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { break; }
                catch (Exception exception)
                {
                    _readerFailures++;
                    SetStatus(_readerFailures >= 3
                        ? "Editor Reader seems to be failing a lot..."
                        : exception.Message);
                    _overlay?.Disable();
                }

            try { await Task.Delay(_runtimeSnapshot is null ? 1000 : 100, cancellationToken).ConfigureAwait(false); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task RefreshOnceCoreAsync(CancellationToken cancellationToken)
    {
        if (!_input.IsSupported)
        {
            SetStatus("Geometry Dashboard requires Windows.");
            _overlay?.Disable();
            return;
        }

        if (!_applicationSettings.UseEditorReader)
        {
            SetStatus("Enable Editor Reader in Preferences to use Geometry Dashboard.");
            _overlay?.Disable();
            return;
        }

        var snapshot = await _runtime.ReadAsync(cancellationToken);
        if (snapshot is null)
        {
            SetStatus("Waiting for an open editor...");
            _runtimeSnapshot = null;
            _overlay?.Disable();
            return;
        }

        var previousSnapshot = _runtimeSnapshot;
        bool shouldUpdateRoots = previousSnapshot is null
                                 || Preferences.UpdateMode switch
                                 {
                                     UpdateMode.AnyChange => true,
                                     UpdateMode.TimeChange => previousSnapshot.Editor.EditorTime != snapshot.Editor.EditorTime,
                                     UpdateMode.OsuActivated => snapshot.Window.IsActivated && !previousSnapshot.Window.IsActivated,
                                     UpdateMode.HotkeyDown => false,
                                     _ => true,
                                 };
        _runtimeSnapshot = snapshot;
        _readerFailures = 0;
        UpdateConverter(snapshot);
        if (shouldUpdateRoots || _input.IsHotkeyDown(Preferences.RefreshHotkey)) UpdateRootObjects(snapshot.Editor);
        if (!snapshot.Window.IsActivated)
        {
            UpdateOverlay(snapshot);
            SetStatus("Waiting for osu! to become active...");
            return;
        }

        UpdateHotkeys();
        UpdateOverlay(snapshot);
        SetStatus(_configurationStatus
                  ?? (_layers.GetAllRelevantObjects().Any()
                      ? $"{DrawableCount} virtual object(s)"
                      : "No visible hit objects."));
        NotifyPropertyChanged(nameof(DrawableCount));
        NotifyPropertyChanged(nameof(SelectedCount));
    }

    private void UpdateConverter(GeometryDashboardRuntimeSnapshot snapshot)
    {
        _configurationStatus = null;
        _converter.OsuWindowPosition = new Vector2(snapshot.Window.Bounds.Left, snapshot.Window.Bounds.Top);
        _converter.ScreenBox = snapshot.PrimaryScreen?.Bounds ?? snapshot.Window.Bounds;
        _converter.DpiMultiplier = snapshot.Window.DpiScale;
        _converter.DpiSourceAvailable = snapshot.Window.DpiSourceAvailable;

        if (!string.IsNullOrWhiteSpace(_applicationSettings.OsuConfigPath))
            try
            {
                var values = ReadConfig(_applicationSettings.OsuConfigPath);
                _converter.Fullscreen = GetBool(values, "Fullscreen", true);
                _converter.Letterboxing = GetBool(values, "Letterboxing", true);
                _converter.OsuResolution = new Vector2(
                    GetDouble(values, _converter.Fullscreen ? "WidthFullscreen" : "Width", _converter.OsuResolution.X),
                    GetDouble(values, _converter.Fullscreen ? "HeightFullscreen" : "Height", _converter.OsuResolution.Y));
                _converter.LetterboxingPosition = new Vector2(
                    GetDouble(values, "LetterboxPositionX", _converter.LetterboxingPosition.X),
                    GetDouble(values, "LetterboxPositionY", _converter.LetterboxingPosition.Y));
            }
            catch (Exception exception)
            {
                _configurationStatus = "Could not read osu! configuration: " + exception.Message;
            }
        else
            _configurationStatus = "Specify your osu! user configuration file in Mapping Tools Preferences.";

        _converter.EditorBoxOffset = Preferences.OverlayOffset;
        _layers.AcceptableDifference = Preferences.AcceptableDifference;
        _layers.SetInceptionLevel(Preferences.InceptionLevel);
    }

    private bool UpdateRootObjects(GeometryDashboardEditorSnapshot editor)
    {
        lock (_stateGate)
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
            var existing = _layers.GetRootRelevantHitObjects().ToArray();
            var removed = existing
                .Where(old => !objects.Any(candidate => SameHitObject(old.HitObject, candidate)))
                .ToArray();
            var added = objects
                .Where(candidate => !existing.Any(old => SameHitObject(old.HitObject, candidate)))
                .ToArray();
            foreach (var oldObject in removed)
                oldObject.Dispose();

            _layers.GetRootLayer().Add(added.Select(candidate => new RelevantHitObject(candidate)));
            bool selectionChanged = SynchronizeRootSelection(
                _layers.GetRootRelevantHitObjects(),
                editor.SelectedHitObjects);
            if (added.Length == 0 && removed.Length == 0 && !selectionChanged) return false;

            _layers.GetRootLayer().GenerateNewObjects(true);
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
                HitObjectComparer.Equals(root.HitObject, selectedHitObject));
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
        lock (_stateGate)
        {
            Vector2 screen;
            lock (Project)
            {
                foreach (var slot in Project.SaveSlots.ToArray())
                {
                    bool isDown = _input.IsHotkeyDown(slot.ProjectHotkey);
                    if (isDown && _activeSaveSlots.Add(slot)) LoadSaveSlot(slot);
                    if (!isDown) _activeSaveSlots.Remove(slot);
                }
            }

            if (_input.IsMouseButtonDown(GeometryDashboardMouseButton.Left) && _input.TryGetCursorPosition(out screen))
            {
                var cursor = _converter.ScreenToEditorCoordinate(screen);
                var selected = _layers.GetRootRelevantHitObjects().Where(objectModel => objectModel.IsSelected).ToArray();
                _heldHitObjects = selected;
                _heldHitObject = selected.OrderBy(objectModel => Vector2.Distance(objectModel.HitObject.Pos, cursor))
                    .FirstOrDefault(objectModel => Vector2.Distance(objectModel.HitObject.Pos, cursor) <= Beatmap.GetHitObjectRadius(_runtimeSnapshot?.Editor.CircleSize ?? 5));
                _heldMouseOffset = _heldHitObject is null ? Vector2.Zero : _heldHitObject.HitObject.Pos - cursor;
            }
            else
            {
                _heldHitObject = null;
                _heldHitObjects = [];
                _heldMouseOffset = Vector2.Zero;
            }

            bool snap = _input.IsHotkeyDown(Preferences.SnapHotkey);
            if (!snap) _lastSnapped = null;
            if (snap && _input.TryGetCursorPosition(out screen))
            {
                var cursor = _converter.ScreenToEditorCoordinate(screen);
                var nearest = GetNearestDrawable(
                    cursor + _heldMouseOffset,
                    heldObjects: _heldHitObjects,
                    specialPriority: static objectModel =>
                        objectModel.IsSelected || objectModel.IsLocked || objectModel.IsInheritable);
                if (nearest is not null)
                {
                    _lastSnapped = nearest;
                    _input.TrySetCursorPosition(_converter.EditorToScreenCoordinate(
                        nearest.NearestPoint(cursor + _heldMouseOffset) - _heldMouseOffset));
                }
            }

            if (_input.IsHotkeyDown(Preferences.SelectHotkey))
                ApplyNearestToggle(_selectedDrawables, static objectModel => objectModel.IsSelected, static (objectModel, value) => objectModel.IsSelected = value);
            else _selectedDrawables.Clear();
            if (_input.IsHotkeyDown(Preferences.LockHotkey))
            {
                ApplyNearestLock();
            }
            else
            {
                _lockedDrawables.Clear();
                _unlockedSomething = false;
            }

            if (_input.IsHotkeyDown(Preferences.InheritHotkey))
                ApplyNearestToggle(_inheritableDrawables, static objectModel => objectModel.IsInheritable, static (objectModel, value) => objectModel.IsInheritable = value);
            else _inheritableDrawables.Clear();
        }
    }

    private void UpdateOverlay(GeometryDashboardRuntimeSnapshot snapshot)
    {
        _overlay ??= _overlayFactory.Create();
        if (!_overlay.IsSupported) return;
        if (_overlay.TargetWindow != snapshot.Window.Id) _overlay.Initialize(snapshot.Window.Id);
        _overlay.Enable();
        var editorBox = _converter.GetEditorBox();
        var frame = BuildFrame();
        _overlay.SetFrame(frame);
        _overlay.SetBorder(Preferences.DebugEnabled);
        _overlay.Update(editorBox, snapshot.Window.DpiScale, snapshot.Window.DpiSourceAvailable);
    }

    private GeometryDashboardOverlayFrame BuildFrame()
    {
        lock (_stateGate)
        {
            if (_runtimeSnapshot is null) return GeometryDashboardOverlayFrame.Empty;
            var drawables = _layers.GetAllRelevantDrawables();
            if (_input.IsHotkeyDown(Preferences.SnapHotkey))
            {
                var viewMode = Preferences.KeyDownViewMode;
                if (!viewMode.HasFlag(ViewMode.Everything))
                {
                    List<IRelevantDrawable> related = [];
                    if (_lastSnapped is not null)
                    {
                        if (viewMode.HasFlag(ViewMode.Parents))
                            related.AddRange(_lastSnapped.GetParentage(int.MaxValue).OfType<IRelevantDrawable>());
                        else if (viewMode.HasFlag(ViewMode.DirectParents)) related.AddRange(_lastSnapped.GetParentage(1).OfType<IRelevantDrawable>());

                        if (viewMode.HasFlag(ViewMode.Children))
                            related.AddRange(_lastSnapped.GetDescendants(int.MaxValue).OfType<IRelevantDrawable>());
                        else if (viewMode.HasFlag(ViewMode.DirectChildren)) related.AddRange(_lastSnapped.GetDescendants(1).OfType<IRelevantDrawable>());
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
                    _converter.ToDpi(_converter.ScaleByRatio(new Vector2(circle.Child.Radius, 0))).X,
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
        var editorBox = _converter.GetEditorBox();
        var relative = _converter.EditorToRelativeCoordinate(editorCoordinate);
        return new Vector2(editorBox.Left + relative.X, editorBox.Top + relative.Y);
    }

    private void ApplyNearestToggle(List<IRelevantDrawable> handled, Func<IRelevantDrawable, bool> read, Action<IRelevantDrawable, bool> write)
    {
        if (!_input.TryGetCursorPosition(out var screen)) return;
        var nearest = GetNearestDrawable(
            _converter.ScreenToEditorCoordinate(screen),
            SelectionRange,
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
        if (!_input.TryGetCursorPosition(out var screen)) return;
        var nearest = GetNearestDrawable(
            _converter.ScreenToEditorCoordinate(screen),
            SelectionRange,
            specialPriority: static objectModel => objectModel.IsLocked);
        if (nearest is null || _lockedDrawables.Contains(nearest)) return;
        if (_lockedDrawables.Count == 0) _lockedToggle = !nearest.IsLocked;
        if (_lockedToggle)
        {
            _layers.GetRootLayer().Add(nearest.GetLockedRelevantObject());
        }
        else if (nearest.IsLocked && !_unlockedSomething)
        {
            nearest.Dispose();
            _unlockedSomething = true;
        }

        _lockedDrawables.Add(nearest);
        Regenerate();
    }

    private IRelevantDrawable? GetNearestDrawable(
        Vector2 cursor,
        double range = double.PositiveInfinity,
        IRelevantObject[]? heldObjects = null,
        Func<IRelevantDrawable, bool>? specialPriority = null)
    {
        lock (_stateGate)
        {
            IRelevantDrawable? nearest = null;
            double best = double.PositiveInfinity;
            foreach (var drawable in _layers.GetAllRelevantDrawables())
            {
                if (heldObjects is not null
                    && drawable.ParentObjects.Count > 0
                    && drawable.ParentObjects.All(parent => parent is RelevantHitObject hit && heldObjects.Contains(hit))) continue;
                double distance = drawable.DistanceTo(cursor);
                if (distance > range) continue;
                distance -= RelevancyBias * Math.Clamp(drawable.Relevancy, 0, 1);
                if (drawable is RelevantPoint) distance -= PointsBias;
                if (specialPriority?.Invoke(drawable) == true) distance -= SpecialBias;
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
        lock (_stateGate)
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
        lock (_stateGate)
        {
            var values = _layers.GetAllRelevantDrawables().ToArray();
            bool target = enable || !disable && values.Any(value => !value.IsLocked);
            if (disable) target = false;

            if (target)
                foreach (var value in values.Where(value => !value.IsLocked))
                    _layers.GetRootLayer().Add(value.GetLockedRelevantObject());
            else
                foreach (var value in values.Where(value => value.IsLocked))
                    value.Dispose();

            Regenerate();
        }
    }

    private void Regenerate()
    {
        lock (_stateGate)
        {
            _layers.GetRootLayer().GenerateNewObjects(true);
            _overlay?.SetFrame(BuildFrame());
            _overlay?.Invalidate();
            NotifyPropertyChanged(nameof(DrawableCount));
            NotifyPropertyChanged(nameof(SelectedCount));
        }
    }

    private void ApplyPreferences()
    {
        lock (_stateGate)
        {
            _converter.EditorBoxOffset = Preferences.OverlayOffset;
            _layers.AcceptableDifference = Preferences.AcceptableDifference;
            _layers.SetInceptionLevel(Preferences.InceptionLevel);
            Regenerate();
        }
    }

    private void SetStatus(string value)
    {
        _dispatcher.Post(() =>
        {
            if (!_disposed) Status = value;
        });
    }

    private void NotifyPropertyChanged(string propertyName)
    {
        _dispatcher.Post(() =>
        {
            if (!_disposed) OnPropertyChanged(propertyName);
        });
    }

    private void LoadSaveSlot(SnappingToolsSaveSlot slot)
    {
        _dispatcher.Post(() =>
        {
            if (_disposed) return;
            lock (_stateGate)
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
        lock (_stateGate)
        {
            _activeSaveSlots.Clear();
        }
    }

    private RelevantObjectCollection GetLockedObjects()
    {
        lock (_stateGate)
        {
            RelevantObjectCollection collection = new();
            foreach (var objectModel in _layers.GetAllRelevantObjects().Where(objectModel => objectModel.IsLocked))
                collection.GetOrCreate(objectModel.GetType()).Add(objectModel.GetLockedRelevantObject());
            return collection;
        }
    }

    private void SetLockedObjects(RelevantObjectCollection objects)
    {
        lock (_stateGate)
        {
            foreach (var values in objects.Values) _layers.GetRootLayer().Add(values.Select(value => value.GetLockedRelevantObject()));
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
        return HitObjectComparer.Equals(first, second);
    }

    private Dictionary<string, string> ReadConfig(string path)
    {
        return new Dictionary<string, string>(
            _files.ReadAllLines(path)
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
    private readonly GeometryDashboardViewModel _owner;

    /// <summary>Creates a generator row.</summary>
    public GeometryDashboardGeneratorViewModel(RelevantObjectsGenerator model, GeometryDashboardViewModel owner)
    {
        Model = model ?? throw new ArgumentNullException(nameof(model));
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
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
        return _owner.ShowGeneratorSettingsAsync(this);
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
