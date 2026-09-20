using Mapping_Tools.Application.Tools.GeometryDashboard.Contracts;
using Mapping_Tools.Desktop.Tools.GeometryDashboard.Models;
using Microsoft.Extensions.Hosting;

namespace Mapping_Tools.Desktop.Tools.GeometryDashboard;

/// <summary>
///     Applies Desktop application and shell lifecycle policy to the dashboard
///     application service.
/// </summary>
public sealed class GeometryDashboardLifecycleCoordinator : IHostedService, IDisposable
{
    private readonly Lock gate = new();
    private readonly GeometryDashboardProject project;
    private readonly IGeometryDashboardService service;
    private bool applicationStarted;
    private bool disposed;
    private bool viewActive;

    /// <summary>Creates a lifecycle coordinator for one project and service.</summary>
    /// <param name="project">The Desktop-owned project containing <c>KeepRunning</c>.</param>
    /// <param name="service">The application service to start and stop.</param>
    public GeometryDashboardLifecycleCoordinator(
        GeometryDashboardProject project,
        IGeometryDashboardService service)
    {
        this.project = project ?? throw new ArgumentNullException(nameof(project));
        this.service = service ?? throw new ArgumentNullException(nameof(service));
    }

    /// <summary>Stops lifecycle coordination and the application service.</summary>
    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;
            disposed = true;
            applicationStarted = false;
            viewActive = false;
            service.Stop();
        }
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ApplicationStarted();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        ApplicationStopping();
        return Task.CompletedTask;
    }

    /// <summary>Marks the Desktop application as started and applies the run policy.</summary>
    public void ApplicationStarted()
    {
        lock (gate)
        {
            ThrowIfDisposed();
            applicationStarted = true;
            ReconcileCore();
        }
    }

    /// <summary>Stops the service as part of Desktop application shutdown.</summary>
    public void ApplicationStopping()
    {
        lock (gate)
        {
            if (disposed) return;
            applicationStarted = false;
            viewActive = false;
            ReconcileCore();
        }
    }

    /// <summary>Marks the Geometry Dashboard view active and applies the run policy.</summary>
    public void ViewActivated()
    {
        lock (gate)
        {
            ThrowIfDisposed();
            viewActive = true;
            ReconcileCore();
        }
    }

    /// <summary>Marks the Geometry Dashboard view inactive and applies the run policy.</summary>
    public void ViewDeactivated()
    {
        lock (gate)
        {
            if (disposed) return;
            viewActive = false;
            ReconcileCore();
        }
    }

    /// <summary>Re-evaluates the run policy after the Desktop project setting changes.</summary>
    public void KeepRunningChanged()
    {
        lock (gate)
        {
            if (disposed) return;
            ReconcileCore();
        }
    }

    private void ReconcileCore()
    {
        bool shouldRun = applicationStarted && (viewActive || project.KeepRunning);
        if (shouldRun) service.Start();
        else service.Stop();
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
    }
}
