using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tools.GeometryDashboard;
using Mapping_Tools.Application.Tools.GeometryDashboard.Contracts;
using Mapping_Tools.Application.Tools.GeometryDashboard.Models;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Settings.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.Tools.GeometryDashboard;

[TestClass]
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
    public async Task RefreshOnceAsync_WhenEditorSelectionChanges_SynchronizesRootSelectionState()
    {
        // Arrange
        HitObject initialHitObject = new("64,96,1000,1,0,0:0:0:0:");
        HitObject selectedHitObject = new("64,96,1000,1,0,0:0:0:0:");
        HitObject finalHitObject = new("64,96,1000,1,0,0:0:0:0:");
        var snapshots = new RuntimeStub(
        [
            CreateRuntimeSnapshot(initialHitObject, 0, []),
            CreateRuntimeSnapshot(selectedHitObject, 0, [selectedHitObject]),
            CreateRuntimeSnapshot(finalHitObject, 0, []),
        ]);
        using var service = CreateService(new InputStub(true), snapshots);

        // Act
        await service.RefreshOnceAsync();
        int unselectedCount = service.State.SelectedCount;
        await service.RefreshOnceAsync();
        int selectedCount = service.State.SelectedCount;
        await service.RefreshOnceAsync();

        // Assert
        unselectedCount.Should().Be(0);
        selectedCount.Should().BeGreaterThan(0);
        service.State.SelectedCount.Should().Be(0);
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
            settings: new ApplicationSettings { UseEditorReader = false });

        // Act
        await service.RefreshOnceAsync();

        // Assert
        service.State.Status.Should().Be("Unable to run: enable Editor Reader in Preferences.");
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

    private static GeometryDashboardService CreateService(
        InputStub input,
        RuntimeStub? runtime = null,
        OverlayStub? overlay = null,
        ApplicationSettings? settings = null)
    {
        return new GeometryDashboardService(
            settings ?? new ApplicationSettings(),
            new GeometryDashboardServiceOptions(),
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

        public bool IsProcessRunning { get; set; } = true;
        public Exception? ReadException { get; set; }
        public GeometryDashboardRuntimeSnapshot? RepeatedSnapshot { get; set; }

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

        public bool IsSupported => isSupported;
        public bool SnapHotkeyDown { get; set; }
        public bool SelectHotkeyDown { get; set; }
        public Vector2 CursorPosition { get; set; }
        public int CursorSetCount => Volatile.Read(ref cursorSetCount);
        public Task CursorSetReached => cursorSetReached.Task;

        public bool IsHotkeyDown(HotkeySettings? hotkey)
        {
            return SnapHotkeyDown && hotkey is { Key: 56, Modifiers: 0 }
                   || SelectHotkeyDown && hotkey is { Key: 57, Modifiers: 0 };
        }

        public bool IsMouseButtonDown(GeometryDashboardMouseButton button)
        {
            return false;
        }

        public bool TryGetCursorPosition(out Vector2 position)
        {
            position = CursorPosition;
            return true;
        }

        public bool TrySetCursorPosition(Vector2 position)
        {
            if (Interlocked.Increment(ref cursorSetCount) >= 4) cursorSetReached.TrySetResult();
            return true;
        }
    }

    private sealed class OverlayStub : IGeometryDashboardOverlayService
    {
        public bool IsSupported => true;
        public bool IsVisible { get; private set; }
        public string? ConfigurationStatus => null;
        public GeometryDashboardOverlayScene LastScene { get; private set; } = GeometryDashboardOverlayScene.Empty;

        public void Update(GeometryDashboardOverlayScene scene, GeometryDashboardOverlayOptions options)
        {
            LastScene = scene;
            IsVisible = true;
        }

        public void Hide() => IsVisible = false;
        public void Dispose() { }
    }
}
