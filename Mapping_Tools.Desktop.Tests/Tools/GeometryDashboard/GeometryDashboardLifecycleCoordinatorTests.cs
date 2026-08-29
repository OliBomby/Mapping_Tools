using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tools.GeometryDashboard;
using Mapping_Tools.Application.Tools.GeometryDashboard.Contracts;
using Mapping_Tools.Application.Tools.GeometryDashboard.Models;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Settings.Models;
using Mapping_Tools.Desktop.Tools.GeometryDashboard;
using Mapping_Tools.Desktop.Tools.GeometryDashboard.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Tools.GeometryDashboard;

[TestClass]
public sealed class GeometryDashboardLifecycleCoordinatorTests
{
    [TestMethod]
    public void ViewLifecycle_WhenKeepRunningIsDisabled_StartsAndStopsServiceWithActivation()
    {
        // Arrange
        GeometryDashboardProject project = new() { KeepRunning = false };
        using var service = CreateService(project);
        using var lifecycle = new GeometryDashboardLifecycleCoordinator(project, service);
        lifecycle.ApplicationStarted();

        // Act
        lifecycle.ViewActivated();
        bool runningWhileActive = service.IsRunning;
        lifecycle.ViewDeactivated();

        // Assert
        runningWhileActive.Should().BeTrue();
        service.IsRunning.Should().BeFalse();
    }

    [TestMethod]
    public void ApplicationLifecycle_WhenKeepRunningIsEnabled_RunsOutsideViewAndStopsAtShutdown()
    {
        // Arrange
        GeometryDashboardProject project = new() { KeepRunning = true };
        using var service = CreateService(project);
        using var lifecycle = new GeometryDashboardLifecycleCoordinator(project, service);

        // Act
        lifecycle.ApplicationStarted();
        bool runningAtApplicationStart = service.IsRunning;
        lifecycle.ViewActivated();
        lifecycle.ViewDeactivated();
        bool runningAfterViewDeactivation = service.IsRunning;
        lifecycle.ApplicationStopping();

        // Assert
        runningAtApplicationStart.Should().BeTrue();
        runningAfterViewDeactivation.Should().BeTrue();
        service.IsRunning.Should().BeFalse();
    }

    [TestMethod]
    public async Task HostedLifecycle_StartAndStop_DelegatesApplicationLifecycleToPolicy()
    {
        // Arrange
        GeometryDashboardProject project = new() { KeepRunning = true };
        using var service = CreateService(project);
        using var lifecycle = new GeometryDashboardLifecycleCoordinator(project, service);

        // Act
        await lifecycle.StartAsync(CancellationToken.None);
        bool runningAfterStart = service.IsRunning;
        await lifecycle.StopAsync(CancellationToken.None);

        // Assert
        runningAfterStart.Should().BeTrue();
        service.IsRunning.Should().BeFalse();
    }

    private static GeometryDashboardService CreateService(GeometryDashboardProject project)
    {
        return new GeometryDashboardService(
            new ApplicationSettings(),
            project,
            new RuntimeStub(),
            new InputStub(),
            new OverlayStub());
    }

    private sealed class RuntimeStub : IGeometryDashboardRuntime
    {
        public Task<GeometryDashboardRuntimeSnapshot?> ReadAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<GeometryDashboardRuntimeSnapshot?>(null);
        }
    }

    private sealed class InputStub : IGeometryDashboardInputService
    {
        public bool IsSupported => true;

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
