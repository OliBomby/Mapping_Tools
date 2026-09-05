using Mapping_Tools.Application.Tools.GeometryDashboard.Models;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators;

namespace Mapping_Tools.Application.Tools.GeometryDashboard.Contracts;

/// <summary>
///     Runs the Geometry Dashboard calculation and interaction session without
///     depending on a particular desktop view or host lifetime.
/// </summary>
public interface IGeometryDashboardService : IDisposable
{
    /// <summary>
    ///     Raised after a calculation or command changes the state exposed by
    ///     <see cref="State" />. Handlers may run on the worker thread.
    /// </summary>
    event EventHandler? StateChanged;

    /// <summary>Gets the generator models owned by the calculation session.</summary>
    IReadOnlyList<RelevantObjectsGenerator> Generators { get; }

    /// <summary>Gets the latest externally observable service state.</summary>
    GeometryDashboardServiceState State { get; }

    /// <summary>Gets whether the calculation worker is currently running.</summary>
    bool IsRunning { get; }

    /// <summary>Starts the calculation worker if it is not already running.</summary>
    void Start();

    /// <summary>
    ///     Requests the calculation worker to stop and waits for its current
    ///     operation to finish.
    /// </summary>
    void Stop();

    /// <summary>Runs one calculation update independently of the worker lifecycle.</summary>
    /// <param name="cancellationToken">Cancels the runtime read or calculation.</param>
    /// <returns>A task that completes after the state has been reconciled.</returns>
    Task RefreshOnceAsync(CancellationToken cancellationToken = default);

    /// <summary>Applies the current project preferences to the calculation graph.</summary>
    void ApplyPreferences();

    /// <summary>Regenerates the calculation graph from its current root objects.</summary>
    void Regenerate();

    /// <summary>Executes the selection toggle using the supplied targeting mode.</summary>
    /// <param name="targetingMode">The state change to apply to targeted objects.</param>
    void ToggleSelected(GeometryDashboardTargetingMode targetingMode = GeometryDashboardTargetingMode.Toggle);

    /// <summary>Executes the lock toggle using the supplied targeting mode.</summary>
    /// <param name="targetingMode">The state change to apply to targeted objects.</param>
    void ToggleLocked(GeometryDashboardTargetingMode targetingMode = GeometryDashboardTargetingMode.Toggle);

    /// <summary>Executes the inheritable toggle using the supplied targeting mode.</summary>
    /// <param name="targetingMode">The state change to apply to targeted objects.</param>
    void ToggleInheritable(GeometryDashboardTargetingMode targetingMode = GeometryDashboardTargetingMode.Toggle);

    /// <summary>Gets detached copies of every currently locked virtual object.</summary>
    /// <returns>A collection suitable for serialization.</returns>
    RelevantObjectCollection GetLockedObjects();

    /// <summary>Adds detached locked objects to the root graph and regenerates it.</summary>
    /// <param name="objects">The detached objects to add.</param>
    void SetLockedObjects(RelevantObjectCollection objects);
}
