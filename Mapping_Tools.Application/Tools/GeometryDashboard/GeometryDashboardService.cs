using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tools.GeometryDashboard.Contracts;
using Mapping_Tools.Application.Tools.GeometryDashboard.Models;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.GeometryDashboard;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectCollection;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorCollection;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorTypes;
using Mapping_Tools.Core.Tools.GeometryDashboard.Serialization;

namespace Mapping_Tools.Application.Tools.GeometryDashboard;

/// <summary>
///     Owns the Geometry Dashboard calculation session, interaction loop, and
///     overlay updates while it is explicitly running.
/// </summary>
public sealed class GeometryDashboardService : IGeometryDashboardService
{
    private const double relevancy_bias = 4;
    private const double points_bias = 3;
    private const double special_bias = 2;
    private const double selection_range = 80;
    private static readonly HitObjectComparer hitObjectComparer = new();

    private readonly GeometryDashboardServiceOptions project;
    private readonly ApplicationSettings applicationSettings;
    private readonly IGeometryDashboardInputService input;
    private readonly LayerCollection layers;
    private readonly IGeometryDashboardOverlayService overlayService;
    private readonly IGeometryDashboardRuntime runtime;
    private readonly object lifecycleGate = new();
    private readonly SemaphoreSlim refreshGate = new(1, 1);
    private readonly object stateGate = new();
    private readonly List<IRelevantDrawable> inheritableDrawables = [];
    private readonly List<IRelevantDrawable> lockedDrawables = [];
    private readonly List<IRelevantDrawable> selectedDrawables = [];
    private GeometryDashboardServiceState state = new(
        "Waiting for osu!...",
        0,
        false,
        0,
        0);
    private CancellationTokenSource? runCancellation;
    private Task? runLoop;
    private GeometryDashboardRuntimeSnapshot? runtimeSnapshot;
    private RelevantHitObject? heldHitObject;
    private IRelevantObject[] heldHitObjects = [];
    private IRelevantDrawable? lastSnapped;
    private Vector2 heldMouseOffset;
    private bool lockedToggle;
    private bool unlockedSomething;
    private int readerFailures;
    private bool disposed;

    /// <summary>
    ///     Creates a dashboard calculation session over the supplied project and
    ///     platform-neutral runtime ports.
    /// </summary>
    /// <param name="applicationSettings">Shared application settings.</param>
    /// <param name="project">The project state whose preferences drive the session.</param>
    /// <param name="runtime">Reads semantic osu! editor snapshots.</param>
    /// <param name="input">Reads and updates osu!-space global input.</param>
    /// <param name="overlayService">Displays neutral osu!-space overlay scenes.</param>
    public GeometryDashboardService(
        ApplicationSettings applicationSettings,
        GeometryDashboardServiceOptions project,
        IGeometryDashboardRuntime runtime,
        IGeometryDashboardInputService input,
        IGeometryDashboardOverlayService overlayService)
    {
        this.applicationSettings = applicationSettings ?? throw new ArgumentNullException(nameof(applicationSettings));
        this.project = project ?? throw new ArgumentNullException(nameof(project));
        this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        this.input = input ?? throw new ArgumentNullException(nameof(input));
        this.overlayService = overlayService ?? throw new ArgumentNullException(nameof(overlayService));

        Generators = DiscoverGenerators();
        project.SetGenerators(Generators);
        layers = new LayerCollection(
            new RelevantObjectsGeneratorCollection(Generators),
            project.CurrentPreferences.AcceptableDifference);
    }

    /// <inheritdoc />
    public event EventHandler? StateChanged;

    /// <inheritdoc />
    public IReadOnlyList<RelevantObjectsGenerator> Generators { get; }

    /// <inheritdoc />
    public GeometryDashboardServiceState State
    {
        get
        {
            lock (stateGate) return state;
        }
    }

    /// <inheritdoc />
    public bool IsRunning
    {
        get
        {
            lock (lifecycleGate) return runLoop is { IsCompleted: false };
        }
    }

    /// <inheritdoc />
    public void Start()
    {
        lock (lifecycleGate)
        {
            ThrowIfDisposed();
            if (runLoop is { IsCompleted: false }) return;

            runCancellation?.Dispose();
            runCancellation = new CancellationTokenSource();
            CancellationToken cancellationToken = runCancellation.Token;
            runLoop = Task.Run(() => RunLoopAsync(cancellationToken));
        }
    }

    /// <inheritdoc />
    public void Stop()
    {
        Task? worker;
        CancellationTokenSource? cancellation;
        lock (lifecycleGate)
        {
            cancellation = runCancellation;
            worker = runLoop;
            runCancellation = null;
            runLoop = null;
            cancellation?.Cancel();
        }

        try
        {
            worker?.GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            cancellation?.Dispose();
        }

        overlayService.Hide();
        PublishState(State.Status);
    }

    /// <inheritdoc />
    public async Task RefreshOnceAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await refreshGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await RefreshOnceCoreAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            refreshGate.Release();
        }
    }

    /// <inheritdoc />
    public void ApplyPreferences()
    {
        lock (stateGate)
        {
            layers.AcceptableDifference = project.CurrentPreferences.AcceptableDifference;
            layers.SetInceptionLevel(project.CurrentPreferences.InceptionLevel);
            layers.GetRootLayer().GenerateNewObjects(true);
            UpdateOverlay();
        }

        PublishState(State.Status);
    }

    /// <inheritdoc />
    public void Regenerate()
    {
        lock (stateGate)
        {
            layers.GetRootLayer().GenerateNewObjects(true);
            UpdateOverlay();
        }

        PublishState(State.Status);
    }

    /// <inheritdoc />
    public void ToggleSelected(GeometryDashboardTargetingMode targetingMode = GeometryDashboardTargetingMode.Toggle)
    {
        ToggleObjects(
            layers.GetAllRelevantDrawables(),
            targetingMode == GeometryDashboardTargetingMode.Enable,
            targetingMode == GeometryDashboardTargetingMode.Disable,
            static objectModel => objectModel.IsSelected,
            static (objectModel, value) => objectModel.IsSelected = value);
    }

    /// <inheritdoc />
    public void ToggleLocked(GeometryDashboardTargetingMode targetingMode = GeometryDashboardTargetingMode.Toggle)
    {
        ToggleLockedObjects(
            targetingMode == GeometryDashboardTargetingMode.Enable,
            targetingMode == GeometryDashboardTargetingMode.Disable);
    }

    /// <inheritdoc />
    public void ToggleInheritable(GeometryDashboardTargetingMode targetingMode = GeometryDashboardTargetingMode.Toggle)
    {
        ToggleObjects(
            layers.GetAllRelevantDrawables(),
            targetingMode == GeometryDashboardTargetingMode.Enable,
            targetingMode == GeometryDashboardTargetingMode.Disable,
            static objectModel => objectModel.IsInheritable,
            static (objectModel, value) => objectModel.IsInheritable = value);
    }

    /// <inheritdoc />
    public RelevantObjectCollection GetLockedObjects()
    {
        lock (stateGate)
        {
            RelevantObjectCollection collection = new();
            foreach (var objectModel in layers.GetAllRelevantObjects().Where(objectModel => objectModel.IsLocked))
            {
                if (!collection.TryGetValue(objectModel.GetType(), out var values))
                {
                    values = [];
                    collection[objectModel.GetType()] = values;
                }

                values.Add(objectModel.GetLockedRelevantObject());
            }

            return collection;
        }
    }

    /// <inheritdoc />
    public void SetLockedObjects(RelevantObjectCollection objects)
    {
        ArgumentNullException.ThrowIfNull(objects);
        lock (stateGate)
        {
            foreach (var values in objects.Values)
                layers.GetRootLayer().Add(values.Select(value => value.GetLockedRelevantObject()));

            layers.GetRootLayer().GenerateNewObjects(true);
            UpdateOverlay();
        }

        PublishState(State.Status);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (lifecycleGate)
        {
            if (disposed) return;
            disposed = true;
        }

        Stop();
        lock (stateGate)
        {
            foreach (var objectModel in layers.GetAllRelevantObjects().ToArray()) objectModel.Dispose();
        }

        refreshGate.Dispose();
        GC.SuppressFinalize(this);
    }

    private GeometryDashboardPreferences Preferences => project.CurrentPreferences;

    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await RefreshOnceAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                readerFailures++;
                PublishState(readerFailures >= 3
                    ? "Editor Reader seems to be failing a lot..."
                    : exception.Message);
                overlayService.Hide();
            }

            try
            {
                await Task.Delay(runtimeSnapshot is null ? 1000 : 100, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task RefreshOnceCoreAsync(CancellationToken cancellationToken)
    {
        if (!input.IsSupported)
        {
            PublishState("Geometry Dashboard requires Windows.");
            overlayService.Hide();
            return;
        }

        if (!applicationSettings.UseEditorReader)
        {
            PublishState("Enable Editor Reader in Preferences to use Geometry Dashboard.");
            overlayService.Hide();
            return;
        }

        var snapshot = await runtime.ReadAsync(cancellationToken).ConfigureAwait(false);
        if (snapshot is null)
        {
            PublishState("Waiting for an open editor...");
            lock (stateGate) runtimeSnapshot = null;
            overlayService.Hide();
            return;
        }

        GeometryDashboardRuntimeSnapshot? previousSnapshot;
        lock (stateGate)
        {
            previousSnapshot = runtimeSnapshot;
            runtimeSnapshot = snapshot;
        }

        bool shouldUpdateRoots = previousSnapshot is null
                                 || Preferences.UpdateMode switch
                                 {
                                     UpdateMode.AnyChange => true,
                                     UpdateMode.TimeChange => previousSnapshot.Editor.EditorTime != snapshot.Editor.EditorTime,
                                     UpdateMode.OsuActivated => snapshot.IsEditorActive && !previousSnapshot.IsEditorActive,
                                     UpdateMode.HotkeyDown => false,
                                     _ => true,
                                 };
        readerFailures = 0;
        UpdatePreferences();
        if (shouldUpdateRoots || input.IsHotkeyDown(Preferences.RefreshHotkey)) UpdateRootObjects(snapshot.Editor);
        if (!snapshot.IsEditorActive)
        {
            UpdateOverlay();
            PublishState("Waiting for osu! to become active...");
            return;
        }

        UpdateHotkeys();
        UpdateOverlay();
        PublishState(overlayService.ConfigurationStatus
                     ?? (layers.GetAllRelevantObjects().Any()
                         ? $"{State.DrawableCount} virtual object(s)"
                         : "No visible hit objects."));
    }

    private void UpdatePreferences()
    {
        lock (stateGate)
        {
            layers.AcceptableDifference = Preferences.AcceptableDifference;
            layers.SetInceptionLevel(Preferences.InceptionLevel);
        }
    }

    private bool UpdateRootObjects(LiveBeatmapSnapshot editor)
    {
        lock (stateGate)
        {
            double approachTime = Beatmap.GetApproachTime(editor.ApproachRate);
            double editorTime = editor.EditorTime ?? 0;
            var candidates = Preferences.SelectedHitObjectMode switch
            {
                SelectedHitObjectMode.OnlySelected => editor.SelectedHitObjects,
                SelectedHitObjectMode.VisibleOrSelected when editor.SelectedHitObjects.Count > 0 =>
                    editor.SelectedHitObjects,
                _ => editor.HitObjects.Where(objectModel =>
                    editorTime > objectModel.Time - approachTime
                    && editorTime < objectModel.EndTime + approachTime),
            };
            var objects = candidates.ToArray();
            var existing = layers.GetRootRelevantHitObjects().ToArray();
            var removed = existing
                .Where(old => !objects.Any(candidate => SameHitObject(old.HitObject, candidate)))
                .ToArray();
            var added = objects
                .Where(candidate => !existing.Any(old => SameHitObject(old.HitObject, candidate)))
                .ToArray();
            foreach (var oldObject in removed) oldObject.Dispose();

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
            Vector2 cursor;
            if (input.IsMouseButtonDown(GeometryDashboardMouseButton.Left)
                && input.TryGetCursorPosition(out cursor))
            {
                var selected = layers.GetRootRelevantHitObjects()
                    .Where(objectModel => objectModel.IsSelected)
                    .ToArray();
                heldHitObjects = selected;
                heldHitObject = selected
                    .OrderBy(objectModel => Vector2.Distance(objectModel.HitObject.Pos, cursor))
                    .FirstOrDefault(objectModel =>
                        Vector2.Distance(objectModel.HitObject.Pos, cursor)
                        <= Beatmap.GetHitObjectRadius(runtimeSnapshot?.Editor.CircleSize ?? 5));
                heldMouseOffset = heldHitObject is null
                    ? Vector2.Zero
                    : heldHitObject.HitObject.Pos - cursor;
            }
            else
            {
                heldHitObject = null;
                heldHitObjects = [];
                heldMouseOffset = Vector2.Zero;
            }

            bool snap = input.IsHotkeyDown(Preferences.SnapHotkey);
            if (!snap) lastSnapped = null;
            if (snap && input.TryGetCursorPosition(out cursor))
            {
                var nearest = GetNearestDrawable(
                    cursor + heldMouseOffset,
                    heldObjects: heldHitObjects,
                    specialPriority: static objectModel =>
                        objectModel.IsSelected || objectModel.IsLocked || objectModel.IsInheritable);
                if (nearest is not null)
                {
                    lastSnapped = nearest;
                    input.TrySetCursorPosition(
                        nearest.NearestPoint(cursor + heldMouseOffset) - heldMouseOffset);
                }
            }

            if (input.IsHotkeyDown(Preferences.SelectHotkey))
                ApplyNearestToggle(
                    selectedDrawables,
                    static objectModel => objectModel.IsSelected,
                    static (objectModel, value) => objectModel.IsSelected = value);
            else selectedDrawables.Clear();

            if (input.IsHotkeyDown(Preferences.LockHotkey)) ApplyNearestLock();
            else
            {
                lockedDrawables.Clear();
                unlockedSomething = false;
            }

            if (input.IsHotkeyDown(Preferences.InheritHotkey))
                ApplyNearestToggle(
                    inheritableDrawables,
                    static objectModel => objectModel.IsInheritable,
                    static (objectModel, value) => objectModel.IsInheritable = value);
            else inheritableDrawables.Clear();
        }
    }

    private void UpdateOverlay()
    {
        if (!overlayService.IsSupported) return;
        overlayService.Update(
            BuildScene(),
            new GeometryDashboardOverlayOptions(
                Preferences.OverlayOffset,
                Preferences.DebugEnabled));
    }

    private GeometryDashboardOverlayScene BuildScene()
    {
        lock (stateGate)
        {
            if (runtimeSnapshot is null) return GeometryDashboardOverlayScene.Empty;
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
                        else if (viewMode.HasFlag(ViewMode.DirectParents))
                            related.AddRange(lastSnapped.GetParentage(1).OfType<IRelevantDrawable>());

                        if (viewMode.HasFlag(ViewMode.Children))
                            related.AddRange(lastSnapped.GetDescendants(int.MaxValue).OfType<IRelevantDrawable>());
                        else if (viewMode.HasFlag(ViewMode.DirectChildren))
                            related.AddRange(lastSnapped.GetDescendants(1).OfType<IRelevantDrawable>());
                    }

                    drawables = related;
                }
            }
            else if (!Preferences.KeyUpViewMode.HasFlag(ViewMode.Everything)) drawables = [];

            List<GeometryDashboardOverlayShape> shapes = [];
            if (Preferences.VisiblePlayfieldBoundary)
                shapes.Add(new GeometryDashboardOverlayShape(
                    GeometryDashboardOverlayShapeKind.Box,
                    new Vector2(-65, -57),
                    new Vector2(576, 423),
                    0,
                    RgbaColour.FromRgb(255, 140, 0),
                    1,
                    1,
                    DashStylesEnum.Solid));

            foreach (var drawable in drawables.Distinct())
            {
                var preferences = Preferences.GetReleventObjectPreferences(drawable.PreferencesName);
                if (drawable.IsSelected) AddDrawableShape(shapes, drawable, preferences, true);
                AddDrawableShape(shapes, drawable, preferences, false);
            }

            return new GeometryDashboardOverlayScene(shapes);
        }
    }

    private static void AddDrawableShape(
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
                    point.Child,
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
                    circle.Child.Centre,
                    default,
                    circle.Child.Radius,
                    colour,
                    opacity,
                    thickness,
                    preferences.Dashstyle));
                break;
            case RelevantLine line when Line2.Intersection(
                                            new Box2(-1000, -1000, 1512, 1384),
                                            line.Child,
                                            out var intersections)
                                        && intersections.Length >= 2:
                shapes.Add(new GeometryDashboardOverlayShape(
                    GeometryDashboardOverlayShapeKind.Line,
                    intersections[0],
                    intersections[1],
                    0,
                    colour,
                    opacity,
                    thickness,
                    preferences.Dashstyle));
                break;
        }
    }

    private void ApplyNearestToggle(
        List<IRelevantDrawable> handled,
        Func<IRelevantDrawable, bool> read,
        Action<IRelevantDrawable, bool> write)
    {
        if (!input.TryGetCursorPosition(out var cursor)) return;
        var nearest = GetNearestDrawable(cursor, selection_range, specialPriority: read);
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
        if (!input.TryGetCursorPosition(out var cursor)) return;
        var nearest = GetNearestDrawable(
            cursor,
            selection_range,
            specialPriority: static objectModel => objectModel.IsLocked);
        if (nearest is null || lockedDrawables.Contains(nearest)) return;
        if (lockedDrawables.Count == 0) lockedToggle = !nearest.IsLocked;
        if (lockedToggle)
            layers.GetRootLayer().Add(nearest.GetLockedRelevantObject());
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
                    && drawable.ParentObjects.All(parent => parent is RelevantHitObject hit && heldObjects.Contains(hit)))
                    continue;

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

            layers.GetRootLayer().GenerateNewObjects(true);
            UpdateOverlay();
        }

        PublishState(State.Status);
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
                foreach (var value in values.Where(value => value.IsLocked)) value.Dispose();

            layers.GetRootLayer().GenerateNewObjects(true);
            UpdateOverlay();
        }

        PublishState(State.Status);
    }

    private IReadOnlyList<RelevantObjectsGenerator> DiscoverGenerators()
    {
        return typeof(RelevantObjectsGenerator).Assembly.GetTypes()
            .Where(type => !type.IsAbstract
                           && typeof(RelevantObjectsGenerator).IsAssignableFrom(type)
                           && type.GetConstructor(Type.EmptyTypes) is not null)
            .Select(type => (RelevantObjectsGenerator)Activator.CreateInstance(type)!)
            .ToArray();
    }

    private void PublishState(string status)
    {
        GeometryDashboardServiceState next;
        EventHandler? handler;
        lock (stateGate)
        {
            next = new(
                status,
                state.Progress,
                runtimeSnapshot is not null && overlayService.IsVisible,
                layers.GetAllRelevantDrawables().Count(),
                layers.GetAllRelevantObjects().Count(objectModel => objectModel.IsSelected));
            state = next;
            handler = StateChanged;
        }

        handler?.Invoke(this, EventArgs.Empty);
    }

    private static bool SameHitObject(HitObject first, HitObject second)
    {
        return hitObjectComparer.Equals(first, second);
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
        return RgbaColour.FromArgb(
            alpha,
            (byte)Math.Clamp((r + m) * 255, 0, 255),
            (byte)Math.Clamp((g + m) * 255, 0, 255),
            (byte)Math.Clamp((b + m) * 255, 0, 255));
    }

    private void ThrowIfDisposed()
    {
        lock (lifecycleGate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
        }
    }
}
