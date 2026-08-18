using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Classes.MathUtil;
using Mapping_Tools.Core.Classes.Tools.SnappingTools.Serialization;

namespace Mapping_Tools.Application.GeometryDashboard;

/// <summary>
/// Identifies a desktop window without exposing the operating system's native
/// handle representation to Application or Core.
/// </summary>
/// <param name="Value">The opaque window identifier supplied by Infrastructure.</param>
public readonly record struct PlatformWindowId(long Value)
{
    /// <summary>
    /// Gets whether the identifier does not refer to a usable native window.
    /// </summary>
    public bool IsEmpty => Value == 0;
}

/// <summary>
/// Describes the stable osu! process selected by the Windows process adapter.
/// </summary>
/// <param name="ProcessId">The operating-system process identifier.</param>
/// <param name="MainWindow">The process's main top-level window identifier.</param>
/// <param name="MainWindowTitle">The title observed when the process was discovered.</param>
public sealed record GeometryDashboardProcess(
    long ProcessId,
    PlatformWindowId MainWindow,
    string MainWindowTitle);

/// <summary>
/// Describes a top-level window in physical desktop pixels.
/// </summary>
/// <param name="Id">The opaque native window identifier.</param>
/// <param name="ProcessId">The owning process identifier, when it was available.</param>
/// <param name="Title">The current window title.</param>
/// <param name="Bounds">The screen-space rectangle, including native window chrome.</param>
/// <param name="IsVisible">Whether Windows reports the window as visible.</param>
/// <param name="IsActivated">Whether the window is the current foreground window.</param>
/// <param name="DpiScale">The horizontal and vertical effective-DPI multipliers for the window.</param>
/// <param name="DpiSourceAvailable">Whether the window DPI was supplied by Windows.</param>
public sealed record GeometryDashboardWindow(
    PlatformWindowId Id,
    long ProcessId,
    string Title,
    Box2 Bounds,
    bool IsVisible,
    bool IsActivated,
    Vector2 DpiScale,
    bool DpiSourceAvailable);

/// <summary>
/// Describes one monitor using physical desktop pixels and its effective DPI.
/// </summary>
/// <param name="Id">The opaque monitor identifier.</param>
/// <param name="Bounds">The complete monitor rectangle, including negative virtual-screen coordinates.</param>
/// <param name="WorkingArea">The monitor rectangle excluding taskbars and other app bars.</param>
/// <param name="IsPrimary">Whether the monitor is the primary desktop monitor.</param>
/// <param name="DpiScale">The horizontal and vertical effective-DPI multipliers, relative to 96 DPI.</param>
/// <param name="DpiSourceAvailable">Whether Windows supplied the monitor DPI instead of the 96-DPI fallback.</param>
public sealed record GeometryDashboardScreen(
    long Id,
    Box2 Bounds,
    Box2 WorkingArea,
    bool IsPrimary,
    Vector2 DpiScale,
    bool DpiSourceAvailable);

/// <summary>
/// Carries the validated editor state needed by Geometry Dashboard without
/// exposing Editor Reader's vendor-specific memory model.
/// </summary>
public sealed class GeometryDashboardEditorSnapshot
{
    /// <summary>
    /// Creates a snapshot of one successful editor-memory read.
    /// </summary>
    /// <param name="path">The full path reconstructed from the configured Songs directory.</param>
    /// <param name="approachRate">The live osu! approach-rate value.</param>
    /// <param name="circleSize">The live osu! circle-size value.</param>
    /// <param name="editorTime">The editor playhead in milliseconds.</param>
    /// <param name="hitObjects">The complete live object list, including selection flags.</param>
    public GeometryDashboardEditorSnapshot(
        string path,
        double approachRate,
        double circleSize,
        int editorTime,
        IReadOnlyList<HitObject> hitObjects)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(hitObjects);

        Path = path;
        ApproachRate = approachRate;
        CircleSize = circleSize;
        EditorTime = editorTime;
        HitObjects = hitObjects.ToArray();
    }

    /// <summary>Gets the full path of the beatmap currently held by the editor.</summary>
    public string Path { get; }

    /// <summary>Gets the live approach-rate value used for visibility calculations.</summary>
    public double ApproachRate { get; }

    /// <summary>Gets the live circle-size value used for hit-object radius calculations.</summary>
    public double CircleSize { get; }

    /// <summary>Gets the live editor playhead in milliseconds.</summary>
    public int EditorTime { get; }

    /// <summary>Gets the complete live hit-object list with editor selection state preserved.</summary>
    public IReadOnlyList<HitObject> HitObjects { get; }
}

/// <summary>
/// Finds the stable osu! process used by Geometry Dashboard.
/// </summary>
public interface IGeometryDashboardProcessDiscovery
{
    /// <summary>
    /// Gets whether the adapter can inspect native processes on this platform.
    /// </summary>
    bool IsSupported { get; }

    /// <summary>
    /// Finds the first process whose executable and product identity match osu! stable.
    /// </summary>
    /// <param name="cancellationToken">Cancels before process enumeration begins.</param>
    /// <returns>The matching process snapshot, or <see langword="null"/> when unavailable.</returns>
    Task<GeometryDashboardProcess?> FindAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Reads the selected osu! editor state needed by Geometry Dashboard.
/// </summary>
public interface IGeometryDashboardEditorReader
{
    /// <summary>
    /// Captures a validated memory snapshot from the active osu! editor.
    /// </summary>
    /// <param name="process">The process snapshot selected for this read.</param>
    /// <param name="cancellationToken">Cancels before or during the memory read.</param>
    /// <returns>
    /// A snapshot when osu! is running with an open editor, or <see langword="null"/>
    /// when the process/editor is unavailable.
    /// </returns>
    Task<GeometryDashboardEditorSnapshot?> ReadGeometryDashboardAsync(
        GeometryDashboardProcess process,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Identifies the pointer buttons that Geometry Dashboard can inspect.
/// </summary>
public enum GeometryDashboardMouseButton
{
    /// <summary>The primary/left pointer button.</summary>
    Left
}

/// <summary>
/// Provides global keyboard, mouse-button, and absolute-cursor state.
/// </summary>
public interface IGeometryDashboardInputService
{
    /// <summary>Gets whether the current platform exposes the required global input APIs.</summary>
    bool IsSupported { get; }

    /// <summary>
    /// Tests a persisted activation or editing hotkey using exact modifier-state matching.
    /// </summary>
    /// <param name="hotkey">The legacy-compatible key and modifier values.</param>
    /// <returns><see langword="true"/> only while the complete combination is held.</returns>
    bool IsHotkeyDown(Hotkey? hotkey);

    /// <summary>
    /// Tests whether a pointer button is currently held.
    /// </summary>
    /// <param name="button">The button to inspect.</param>
    /// <returns><see langword="true"/> while the button is held.</returns>
    bool IsMouseButtonDown(GeometryDashboardMouseButton button);

    /// <summary>
    /// Attempts to read the absolute cursor position in physical desktop pixels.
    /// </summary>
    /// <param name="position">Receives the cursor position when the read succeeds.</param>
    /// <returns><see langword="true"/> when Windows supplied a position.</returns>
    bool TryGetCursorPosition(out Vector2 position);

    /// <summary>
    /// Attempts to move the absolute cursor in physical desktop pixels.
    /// </summary>
    /// <param name="position">The destination; fractional values are rounded like the legacy adapter.</param>
    /// <returns><see langword="true"/> when Windows accepted the move.</returns>
    bool TrySetCursorPosition(Vector2 position);
}

/// <summary>
/// Enumerates monitors and supplies their physical bounds and effective DPI.
/// </summary>
public interface IGeometryDashboardScreenService
{
    /// <summary>Gets whether native monitor enumeration is available.</summary>
    bool IsSupported { get; }

    /// <summary>Gets all monitors in the current virtual desktop.</summary>
    IReadOnlyList<GeometryDashboardScreen> GetScreens();

    /// <summary>Gets the primary monitor, or <see langword="null"/> when unavailable.</summary>
    GeometryDashboardScreen? GetPrimaryScreen();

    /// <summary>
    /// Gets the monitor containing a window's nearest monitor area.
    /// </summary>
    /// <param name="window">The window whose monitor should be selected.</param>
    /// <returns>The containing monitor, or <see langword="null"/> when unavailable.</returns>
    GeometryDashboardScreen? GetScreenForWindow(PlatformWindowId window);
}

/// <summary>
/// Tracks top-level windows without leaking native window handles.
/// </summary>
public interface IGeometryDashboardWindowService
{
    /// <summary>Gets whether native window inspection is available.</summary>
    bool IsSupported { get; }

    /// <summary>Gets a current snapshot for a window identifier.</summary>
    /// <param name="window">The window identifier.</param>
    /// <returns>The current window, or <see langword="null"/> when it no longer exists.</returns>
    GeometryDashboardWindow? GetWindow(PlatformWindowId window);

    /// <summary>Gets the current main window for a discovered process.</summary>
    /// <param name="process">The process snapshot whose window should be tracked.</param>
    /// <returns>The current main window, or <see langword="null"/> when unavailable.</returns>
    GeometryDashboardWindow? GetMainWindow(GeometryDashboardProcess process);

    /// <summary>Enumerates current top-level windows in native enumeration order.</summary>
    /// <returns>Window snapshots that could be read successfully.</returns>
    IReadOnlyList<GeometryDashboardWindow> GetTopLevelWindows();
}

/// <summary>
/// Owns the target-bound overlay window lifecycle used by Geometry Dashboard.
/// </summary>
public interface IGeometryDashboardOverlayHost : IDisposable
{
    /// <summary>Gets whether this host can create and control a native overlay.</summary>
    bool IsSupported { get; }

    /// <summary>Gets whether the overlay is currently visible.</summary>
    bool IsVisible { get; }

    /// <summary>Gets the target window currently followed by the overlay.</summary>
    PlatformWindowId? TargetWindow { get; }

    /// <summary>
    /// Creates or retargets the transparent overlay to a top-level window.
    /// </summary>
    /// <param name="targetWindow">The window whose activation controls visibility.</param>
    void Initialize(PlatformWindowId targetWindow);

    /// <summary>Enables target activation tracking and overlay updates.</summary>
    void Enable();

    /// <summary>Disables tracking and hides the overlay.</summary>
    void Disable();

    /// <summary>
    /// Updates the overlay bounds from physical screen pixels while preserving
    /// the legacy DPI conversion and no-source fallback.
    /// </summary>
    /// <param name="physicalBounds">The editor rectangle in physical screen pixels.</param>
    /// <param name="dpiMultiplier">The device-to-logical scale used by the host window.</param>
    /// <param name="dpiSourceAvailable">Whether <paramref name="dpiMultiplier"/> came from a live window DPI source.</param>
    void Update(Box2 physicalBounds, Vector2 dpiMultiplier, bool dpiSourceAvailable);

    /// <summary>Changes the legacy debug border state.</summary>
    /// <param name="enabled">Whether a green-yellow border should be shown.</param>
    void SetBorder(bool enabled);

    /// <summary>Requests a redraw of the platform overlay surface.</summary>
    void Invalidate();
}

/// <summary>Creates target-bound Geometry Dashboard overlay hosts.</summary>
public interface IGeometryDashboardOverlayHostFactory
{
    /// <summary>Creates a disposable overlay host, including an unavailable-platform no-op host.</summary>
    /// <returns>A host whose <see cref="IGeometryDashboardOverlayHost.IsSupported"/> reports platform availability.</returns>
    IGeometryDashboardOverlayHost Create();
}
