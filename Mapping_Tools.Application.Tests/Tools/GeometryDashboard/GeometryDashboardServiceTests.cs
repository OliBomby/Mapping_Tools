using System.Diagnostics.CodeAnalysis;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tools.GeometryDashboard;
using Mapping_Tools.Application.Tools.GeometryDashboard.Contracts;
using Mapping_Tools.Application.Tools.GeometryDashboard.Models;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Settings.Models;
using Mapping_Tools.Core.Tools.GeometryDashboard.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.Tools.GeometryDashboard;

[TestClass]
[SuppressMessage("ReSharper", "AccessToDisposedClosure")]
public sealed class GeometryDashboardServiceTests
{
    [TestMethod]
    public async Task RefreshOnceAsync_WhenInputPlatformIsUnavailable_ReportsGracefulStatus()
    {
        // Arrange
        using var service = CreateService(new InputStub(false));

        // Act
        await service.RefreshOnceAsync();

        // Assert
        service.State.Status.Should().Be("Unable to run: Geometry Dashboard requires Windows.");
    }

    [TestMethod]
    public async Task RefreshOnceAsync_WhenEditorSelectionChangesWithoutConfiguredRefresh_WaitsForConfiguredUpdate()
    {
        // Arrange
        HitObject initialHitObject = new("64,96,1000,1,0,0:0:0:0:");
        HitObject selectedHitObject = new("64,96,1000,1,0,0:0:0:0:");
        HitObject finalHitObject = new("64,96,1000,1,0,0:0:0:0:");
        var snapshots = new RuntimeStub(CreateRuntimeSnapshot(initialHitObject, 0, []), CreateRuntimeSnapshot(selectedHitObject, 0, [selectedHitObject]),
            CreateRuntimeSnapshot(finalHitObject, 1, [finalHitObject]));
        GeometryDashboardServiceOptions project = new();
        project.CurrentPreferences.UpdateMode = UpdateMode.TimeChange;
        using var service = CreateService(new InputStub(true), snapshots, project: project);

        // Act
        await service.RefreshOnceAsync();
        int unselectedCount = service.State.SelectedCount;
        await service.RefreshOnceAsync();
        int selectedCount = service.State.SelectedCount;
        await service.RefreshOnceAsync();
        int refreshedSelectedCount = service.State.SelectedCount;

        // Assert
        unselectedCount.Should().Be(0);
        selectedCount.Should().Be(0);
        refreshedSelectedCount.Should().BeGreaterThan(0);
    }

    [TestMethod]
    public async Task RefreshOnceAsync_WhenVirtualPointsAreSelectedWithoutTimeChange_ReconcilesGeneratedChildrenImmediately()
    {
        // Arrange
        HitObject first = new("64,96,1000,1,0,0:0:0:0:");
        HitObject second = new("320,192,1000,1,0,0:0:0:0:");
        var snapshot = CreateRuntimeSnapshot([first, second], 0, []);
        var runtime = new RuntimeStub(snapshot) { RepeatedSnapshot = snapshot };
        var input = new InputStub(true);
        var overlay = new OverlayStub();
        using var service = CreateService(input, runtime, overlay);

        // Act
        await service.RefreshOnceAsync();
        int initialLineCount = CountLineShapes(overlay.LastScene);
        input.SelectHotkeyDown = true;
        input.CursorPosition = first.Pos;
        await service.RefreshOnceAsync();
        input.CursorPosition = second.Pos;
        await service.RefreshOnceAsync();
        int selectedLineCount = CountLineShapes(overlay.LastScene);
        input.SelectHotkeyDown = false;
        await service.RefreshOnceAsync();
        input.SelectHotkeyDown = true;
        input.CursorPosition = first.Pos;
        await service.RefreshOnceAsync();
        int clearedLineCount = CountLineShapes(overlay.LastScene);

        // Assert
        initialLineCount.Should().Be(0);
        selectedLineCount.Should().BeGreaterThan(0);
        clearedLineCount.Should().Be(0);
    }

    [TestMethod]
    public void Start_WhenFollowedByStop_StopsTheCalculationWorker()
    {
        // Arrange
        using var service = CreateService(new InputStub(true));

        // Act
        service.Start();
        bool running = service.IsRunning;
        service.Stop();

        // Assert
        running.Should().BeTrue();
        service.IsRunning.Should().BeFalse();
        service.State.Status.Should().Be("Stopped");
        service.State.IsConnected.Should().BeFalse();
    }

    [TestMethod]
    [DataRow(false, "Waiting for osu! to start...")]
    [DataRow(true, "Waiting for the osu! editor...")]
    public async Task RefreshOnceAsync_AfterEditorCloses_ReportsMissingDependencyAndDisconnects(
        bool processRunning, string expectedStatus)
    {
        // Arrange
        var runtime = new RuntimeStub(CreateRuntimeSnapshot(new HitObject("64,96,1000,1,0,0:0:0:0:"), 0, []))
        {
            IsProcessRunning = processRunning,
        };
        using var service = CreateService(new InputStub(true), runtime);
        await service.RefreshOnceAsync();
        service.State.IsConnected.Should().BeTrue();

        // Act
        await service.RefreshOnceAsync();

        // Assert
        service.State.Status.Should().Be(expectedStatus);
        service.State.IsConnected.Should().BeFalse();
    }

    [TestMethod]
    public async Task RefreshOnceAsync_WithEditorReaderDisabled_ExplainsHowToEnableService()
    {
        // Arrange
        using var service = CreateService(new InputStub(true),
            settings: new ApplicationSettings { BeatmapLiveStateReading = BeatmapLiveStateReadingMode.Disabled });

        // Act
        await service.RefreshOnceAsync();

        // Assert
        service.State.Status.Should().Be("Unable to run: enable beatmap live-state reading in Preferences.");
        service.State.IsConnected.Should().BeFalse();
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task RefreshOnceAsync_WithEditorSnapshot_ReportsRunningOrUnfocusedState(bool active)
    {
        // Arrange
        var editor = CreateRuntimeSnapshot(new HitObject("64,96,1000,1,0,0:0:0:0:"), 0, []).Editor;
        var snapshot = new GeometryDashboardRuntimeSnapshot(editor, active);
        using var service = CreateService(new InputStub(true), new RuntimeStub(snapshot));

        // Act
        await service.RefreshOnceAsync();

        // Assert
        service.State.Status.Should().Be(active
            ? $"Running: {service.State.DrawableCount} virtual object(s)"
            : $"Unfocused: {service.State.DrawableCount} virtual object(s)");
    }

    [TestMethod]
    public async Task Start_WhenRuntimeReadFails_ReportsErrorAndRecoversOnNextRead()
    {
        // Arrange
        var runtime = new RuntimeStub { ReadException = new InvalidOperationException("Reader unavailable.") };
        using var service = CreateService(new InputStub(true), runtime);
        var error = new TaskCompletionSource<GeometryDashboardServiceState>(TaskCreationOptions.RunContinuationsAsynchronously);
        var recovered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        service.StateChanged += (_, _) =>
        {
            var state = service.State;
            if (state.Status.StartsWith("Error:", StringComparison.Ordinal))
            {
                runtime.ReadException = null;
                error.TrySetResult(state);
            }
            else if (state.Status == "Waiting for the osu! editor...")
            {
                recovered.TrySetResult();
            }
        };

        // Act
        service.Start();
        var errorState = await error.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await recovered.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Assert
        errorState.Status.Should().Be("Error: Reader unavailable. Retrying...");
        errorState.IsConnected.Should().BeFalse();
        service.State.Status.Should().Be("Waiting for the osu! editor...");
    }

    [TestMethod]
    public async Task Start_WhenSnapHotkeyIsHeld_UpdatesCursorAtLegacyFrequency()
    {
        // Arrange
        var input = new InputStub(true) { SnapHotkeyDown = true };
        var snapshot = CreateRuntimeSnapshot(
            new HitObject("64,96,1000,1,0,0:0:0:0:"),
            0,
            []);
        var runtime = new RuntimeStub(snapshot) { RepeatedSnapshot = snapshot };
        using var service = CreateService(input, runtime);

        // Act
        service.Start();
        await input.CursorSetReached.WaitAsync(TimeSpan.FromMilliseconds(250));
        service.Stop();

        // Assert
        input.CursorSetCount.Should().BeGreaterThanOrEqualTo(4);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task Start_WhenDraggingOffCenter_KeepsGrabOffsetAcrossSnappingTicks(bool multipleSelected)
    {
        // Arrange
        HitObject held = new("64,96,1000,1,0,0:0:0:0:");
        HitObject alsoHeld = new("140,96,1050,1,0,0:0:0:0:");
        HitObject target = new("200,96,1100,1,0,0:0:0:0:");
        var snapshot = multipleSelected
            ? CreateRuntimeSnapshot([held, alsoHeld, target], 0, [held, alsoHeld])
            : CreateRuntimeSnapshot([held, target], 0, [held]);
        var runtime = new RuntimeStub(snapshot) { RepeatedSnapshot = snapshot };
        var input = new InputStub(true)
        {
            SnapHotkeyDown = true,
            LeftMouseButtonDown = true,
            CursorPosition = new Vector2(74, 90),
        };
        using var service = CreateService(input, runtime);

        // Act
        service.Start();
        await input.CursorSetReached.WaitAsync(TimeSpan.FromSeconds(5));
        service.Stop();

        // Assert
        input.SetPositions.Should().HaveCountGreaterThanOrEqualTo(4)
            .And.AllBeEquivalentTo(new Vector2(210, 90));
    }

    [TestMethod]
    public async Task Start_WhenDragMovesSinceLastRuntimeRead_RefreshesBeforeCapturingGrabOffset()
    {
        // Arrange
        HitObject original = new("64,96,1000,1,0,0:0:0:0:");
        HitObject moved = new("120,96,1000,1,0,0:0:0:0:");
        HitObject target = new("200,96,1100,1,0,0:0:0:0:");
        var snapshot = CreateRuntimeSnapshot([original, target], 0, [original]);
        var liveSnapshot = CreateRuntimeSnapshot([moved, target], 0, [moved]);
        var runtime = new RuntimeStub(snapshot) { RepeatedSnapshot = snapshot };
        var input = new InputStub(true)
        {
            SnapHotkeyDown = true,
            LeftMouseButtonDown = true,
            CursorPosition = new Vector2(130, 90),
            MouseButtonRead = () => runtime.RepeatedSnapshot = liveSnapshot,
        };
        using var service = CreateService(input, runtime);
        await service.RefreshOnceAsync();

        // Act
        service.Start();
        await input.CursorSetReached.WaitAsync(TimeSpan.FromSeconds(5));
        service.Stop();

        // Assert
        input.SetPositions.Should().HaveCountGreaterThanOrEqualTo(4)
            .And.AllBeEquivalentTo(new Vector2(210, 90));
    }

    [TestMethod]
    [DataRow(UpdateMode.TimeChange)]
    [DataRow(UpdateMode.HotkeyDown)]
    public async Task Start_WhenDraggingAfterSelectionAndPositionChange_UsesLiveSelectionAndExcludesHeldGeometry(
        UpdateMode updateMode)
    {
        // Arrange
        HitObject original = new("64,96,1000,1,0,0:0:0:0:");
        HitObject moved = new("120,96,1000,1,0,0:0:0:0:");
        HitObject target = new("200,96,1100,1,0,0:0:0:0:");
        var liveSnapshot = CreateRuntimeSnapshot([moved, target], 0, [moved]);
        var runtime = new RuntimeStub(CreateRuntimeSnapshot([original, target], 0, []), liveSnapshot)
        {
            RepeatedSnapshot = liveSnapshot,
        };
        var input = new InputStub(true)
        {
            SnapHotkeyDown = true,
            LeftMouseButtonDown = true,
            CursorPosition = new Vector2(130, 90),
        };
        GeometryDashboardServiceOptions project = new();
        project.CurrentPreferences.UpdateMode = updateMode;
        using var service = CreateService(input, runtime, project: project);
        await service.RefreshOnceAsync();
        await service.RefreshOnceAsync();

        // Act
        service.Start();
        await input.CursorSetReached.WaitAsync(TimeSpan.FromSeconds(5));
        service.Stop();

        // Assert
        input.SetPositions.Should().HaveCountGreaterThanOrEqualTo(4)
            .And.AllBeEquivalentTo(new Vector2(210, 90));
    }

    [TestMethod]
    [DataRow(false, true, 74)]
    [DataRow(true, false, 74)]
    [DataRow(true, true, 110)]
    public async Task Start_WithoutAnObjectHeldUnderCursor_SnapsCursorWithoutOffset(
        bool mouseDown, bool selected, int cursorX)
    {
        // Arrange
        HitObject hitObject = new("64,96,1000,1,0,0:0:0:0:");
        var snapshot = CreateRuntimeSnapshot(hitObject, 0, selected ? [hitObject] : []);
        var runtime = new RuntimeStub(snapshot) { RepeatedSnapshot = snapshot };
        var input = new InputStub(true)
        {
            SnapHotkeyDown = true,
            LeftMouseButtonDown = mouseDown,
            CursorPosition = new Vector2(cursorX, 96),
        };
        using var service = CreateService(input, runtime);

        // Act
        service.Start();
        await input.CursorSetReached.WaitAsync(TimeSpan.FromSeconds(5));
        service.Stop();

        // Assert
        input.SetPositions.Should().HaveCountGreaterThanOrEqualTo(4)
            .And.AllBeEquivalentTo(hitObject.Pos);
    }

    [TestMethod]
    public async Task Start_WhenMouseIsReleasedAndAnotherObjectIsGrabbed_ResetsAndRecapturesOffset()
    {
        // Arrange
        HitObject held = new("64,96,1000,1,0,0:0:0:0:");
        HitObject target = new("200,96,1100,1,0,0:0:0:0:");
        var snapshot = CreateRuntimeSnapshot([held, target], 0, [held]);
        var runtime = new RuntimeStub(snapshot) { RepeatedSnapshot = snapshot };
        var input = new InputStub(true)
        {
            SnapHotkeyDown = true,
            LeftMouseButtonDown = true,
            CursorPosition = new Vector2(74, 90),
        };
        input.CursorSet = count =>
        {
            if (count == 1) input.LeftMouseButtonDown = false;
            if (count == 2)
            {
                input.LeftMouseButtonDown = true;
                input.CursorPosition = new Vector2(59, 100);
            }
        };
        using var service = CreateService(input, runtime);

        // Act
        service.Start();
        await input.CursorSetReached.WaitAsync(TimeSpan.FromSeconds(5));
        service.Stop();

        // Assert
        input.SetPositions.Take(4).Should().Equal(
            new Vector2(210, 90), new Vector2(200, 96), new Vector2(195, 100), new Vector2(195, 100));
    }

    private static GeometryDashboardService CreateService(
        InputStub input,
        RuntimeStub? runtime = null,
        OverlayStub? overlay = null,
        ApplicationSettings? settings = null,
        GeometryDashboardServiceOptions? project = null)
    {
        return new GeometryDashboardService(
            settings ?? new ApplicationSettings(),
            project ?? new GeometryDashboardServiceOptions(),
            runtime ?? new RuntimeStub(),
            input,
            overlay ?? new OverlayStub());
    }

    private static GeometryDashboardRuntimeSnapshot CreateRuntimeSnapshot(
        HitObject hitObject,
        int editorTime,
        IReadOnlyList<HitObject> selectedHitObjects)
    {
        return CreateRuntimeSnapshot([hitObject], editorTime, selectedHitObjects);
    }

    private static GeometryDashboardRuntimeSnapshot CreateRuntimeSnapshot(
        IReadOnlyList<HitObject> hitObjects,
        int editorTime,
        IReadOnlyList<HitObject> selectedHitObjects)
    {
        return new GeometryDashboardRuntimeSnapshot(
            new LiveBeatmapSnapshot(
                "C:/Songs/map/map.osu",
                [],
                [],
                hitObjects,
                0,
                1.4,
                1,
                5,
                4,
                editorTime,
                selectedHitObjects),
            true);
    }

    private static int CountLineShapes(GeometryDashboardOverlayScene scene)
    {
        return scene.Shapes.Count(shape => shape.Kind == GeometryDashboardOverlayShapeKind.Line);
    }

    private sealed class RuntimeStub(params GeometryDashboardRuntimeSnapshot?[] snapshots)
        : IGeometryDashboardRuntime
    {
        private readonly Queue<GeometryDashboardRuntimeSnapshot?> snapshots = new(snapshots);
        public Exception? ReadException { get; set; }
        public GeometryDashboardRuntimeSnapshot? RepeatedSnapshot { get; set; }

        public bool IsProcessRunning { get; set; } = true;

        public Task<GeometryDashboardRuntimeSnapshot?> ReadAsync(
            CancellationToken cancellationToken = default)
        {
            if (ReadException is { } exception) throw exception;
            return Task.FromResult(snapshots.Count == 0 ? RepeatedSnapshot : snapshots.Dequeue());
        }
    }

    private sealed class InputStub(bool isSupported) : IGeometryDashboardInputService
    {
        private readonly TaskCompletionSource cursorSetReached = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        private int cursorSetCount;
        public bool SnapHotkeyDown { get; set; }
        public bool SelectHotkeyDown { get; set; }
        public bool LeftMouseButtonDown { get; set; }
        public Vector2 CursorPosition { get; set; }
        public List<Vector2> SetPositions { get; } = [];
        public Action<int>? CursorSet { get; set; }
        public Action? MouseButtonRead { get; set; }
        public int CursorSetCount => Volatile.Read(ref cursorSetCount);
        public Task CursorSetReached => cursorSetReached.Task;

        public bool IsSupported => isSupported;

        public bool IsHotkeyDown(HotkeySettings? hotkey)
        {
            return SnapHotkeyDown && hotkey is { Key: 56, Modifiers: 0 }
                   || SelectHotkeyDown && hotkey is { Key: 57, Modifiers: 0 };
        }

        public bool IsMouseButtonDown(GeometryDashboardMouseButton button)
        {
            MouseButtonRead?.Invoke();
            return button == GeometryDashboardMouseButton.Left && LeftMouseButtonDown;
        }

        public bool TryGetCursorPosition(out Vector2 position)
        {
            position = CursorPosition;
            return true;
        }

        public bool TrySetCursorPosition(Vector2 position)
        {
            CursorPosition = position;
            SetPositions.Add(position);
            int count = Interlocked.Increment(ref cursorSetCount);
            CursorSet?.Invoke(count);
            if (count >= 4) cursorSetReached.TrySetResult();
            return true;
        }
    }

    private sealed class OverlayStub : IGeometryDashboardOverlayService
    {
        public GeometryDashboardOverlayScene LastScene { get; private set; } = GeometryDashboardOverlayScene.Empty;
        public bool IsSupported => true;
        public bool IsVisible { get; private set; }
        public string? ConfigurationStatus => null;

        public void Update(GeometryDashboardOverlayScene scene, GeometryDashboardOverlayOptions options)
        {
            LastScene = scene;
            IsVisible = true;
        }

        public void Hide()
        {
            IsVisible = false;
        }

        public void Dispose() { }
    }
}
