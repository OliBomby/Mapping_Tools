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
        service.State.Status.Should().Be("Geometry Dashboard requires Windows.");
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
    }

    private static GeometryDashboardService CreateService(
        InputStub input,
        RuntimeStub? runtime = null)
    {
        return new GeometryDashboardService(
            new ApplicationSettings(),
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

        public Task<GeometryDashboardRuntimeSnapshot?> ReadAsync(
            CancellationToken cancellationToken = default)
        {
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
        public bool IsSupported => false;
        public bool IsVisible => false;
        public string? ConfigurationStatus => null;
        public void Update(GeometryDashboardOverlayScene scene, GeometryDashboardOverlayOptions options) { }
        public void Hide() { }
        public void Dispose() { }
    }
}
