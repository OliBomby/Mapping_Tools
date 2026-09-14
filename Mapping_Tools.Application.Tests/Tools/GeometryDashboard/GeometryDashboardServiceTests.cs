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
            CreateRuntimeSnapshot(selectedHitObject, 1, [selectedHitObject]),
            CreateRuntimeSnapshot(finalHitObject, 2, []),
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
    public async Task RefreshOnceAsync_WithEditorSnapshot_ReportsRunningOrWaitingForFocus(bool active)
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
            : "Waiting for osu! to become active...");
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

    private static GeometryDashboardService CreateService(
        InputStub input,
        RuntimeStub? runtime = null,
        ApplicationSettings? settings = null)
    {
        return new GeometryDashboardService(
            settings ?? new ApplicationSettings(),
            new GeometryDashboardServiceOptions(),
            runtime ?? new RuntimeStub(),
            input,
            new OverlayStub());
    }

    private static GeometryDashboardRuntimeSnapshot CreateRuntimeSnapshot(
        HitObject hitObject,
        int editorTime,
        IReadOnlyList<HitObject> selectedHitObjects)
    {
        return new GeometryDashboardRuntimeSnapshot(
            new LiveBeatmapSnapshot(
                "C:/Songs/map/map.osu",
                [],
                [],
                [hitObject],
                0,
                1.4,
                1,
                5,
                4,
                editorTime,
                selectedHitObjects),
            true);
    }

    private sealed class RuntimeStub(params GeometryDashboardRuntimeSnapshot?[] snapshots)
        : IGeometryDashboardRuntime
    {
        private readonly Queue<GeometryDashboardRuntimeSnapshot?> snapshots = new(snapshots);

        public bool IsProcessRunning { get; set; } = true;
        public Exception? ReadException { get; set; }

        public Task<GeometryDashboardRuntimeSnapshot?> ReadAsync(
            CancellationToken cancellationToken = default)
        {
            if (ReadException is { } exception) throw exception;
            return Task.FromResult(snapshots.Count == 0 ? null : snapshots.Dequeue());
        }
    }

    private sealed class InputStub(bool isSupported) : IGeometryDashboardInputService
    {
        public bool IsSupported => isSupported;

        public bool IsHotkeyDown(HotkeySettings? hotkey)
        {
            return false;
        }

        public bool IsMouseButtonDown(GeometryDashboardMouseButton button)
        {
            return false;
        }

        public bool TryGetCursorPosition(out Vector2 position)
        {
            position = Vector2.Zero;
            return false;
        }

        public bool TrySetCursorPosition(Vector2 position)
        {
            return false;
        }
    }

    private sealed class OverlayStub : IGeometryDashboardOverlayService
    {
        public bool IsSupported => true;
        public bool IsVisible { get; private set; }
        public string? ConfigurationStatus => null;
        public void Update(GeometryDashboardOverlayScene scene, GeometryDashboardOverlayOptions options) => IsVisible = true;
        public void Hide() => IsVisible = false;
        public void Dispose() { }
    }
}
