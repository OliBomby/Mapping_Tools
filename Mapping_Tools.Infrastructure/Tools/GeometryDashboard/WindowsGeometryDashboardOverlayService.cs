using System.ComponentModel;
using System.Runtime.InteropServices;
using Mapping_Tools.Application.Tools.GeometryDashboard.Contracts;
using Mapping_Tools.Application.Tools.GeometryDashboard.Models;
using Mapping_Tools.Infrastructure.Platform;
using Mapping_Tools.Infrastructure.Tools.GeometryDashboard.Contracts;

namespace Mapping_Tools.Infrastructure.Tools.GeometryDashboard;

/// <summary>
///     Resolves live desktop state and renders neutral Geometry Dashboard scenes
///     through one reusable Windows overlay.
/// </summary>
public sealed class WindowsGeometryDashboardOverlayService : IGeometryDashboardOverlayService
{
    private const uint window_action_message = 0x8000;
    private readonly WindowsGeometryDashboardCoordinateContext coordinates;
    private readonly WindowsGeometryDashboardOverlayHost host;
    private readonly Func<bool> isWindows;
    private readonly Lock gate = new();
    private readonly TaskCompletionSource<uint> windowThreadId = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private Thread? windowThread;
    private Action? windowAction;
    private TaskCompletionSource? windowCompletion;
    private long? targetProcessId;
    private bool disposed;

    /// <summary>Creates the overlay service from the shared coordinate context.</summary>
    /// <param name="coordinates">Refreshes immutable transforms from live platform state.</param>
    /// <param name="windows">Reads the target window followed by the native overlay.</param>
    public WindowsGeometryDashboardOverlayService(
        WindowsGeometryDashboardCoordinateContext coordinates,
        IGeometryDashboardWindowService windows)
        : this(coordinates, windows, OperatingSystem.IsWindows)
    {
    }

    internal WindowsGeometryDashboardOverlayService(
        WindowsGeometryDashboardCoordinateContext coordinates,
        IGeometryDashboardWindowService windows,
        Func<bool> isWindows)
    {
        this.coordinates = coordinates ?? throw new ArgumentNullException(nameof(coordinates));
        this.isWindows = isWindows ?? throw new ArgumentNullException(nameof(isWindows));
        host = new WindowsGeometryDashboardOverlayHost(
            windows ?? throw new ArgumentNullException(nameof(windows)),
            isWindows);
    }

    /// <inheritdoc />
    public bool IsSupported
    {
        get
        {
            lock (gate) {
                return !disposed && isWindows();
            }
        }
    }

    /// <inheritdoc />
    public bool IsVisible => host.IsVisible;

    /// <inheritdoc />
    public string? ConfigurationStatus => coordinates.ConfigurationStatus;

    /// <inheritdoc />
    public void Update(
        GeometryDashboardOverlayScene scene,
        GeometryDashboardOverlayOptions options)
    {
        ArgumentNullException.ThrowIfNull(scene);
        ArgumentNullException.ThrowIfNull(options);
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);

            if (!IsSupported || !coordinates.TryRefresh(options.EditorBoxOffset, out var snapshot))
            {
                if (windowThread is not null) InvokeWindow(host.Disable);
                return;
            }

            InvokeWindow(() =>
            {
                if (host.TargetWindow != snapshot.Window.Id
                    || targetProcessId != snapshot.Window.ProcessId
                    || !host.HasNativeWindow)
                {
                    host.Initialize(snapshot.Window.Id);
                    targetProcessId = snapshot.Window.ProcessId;
                }

                host.SetScene(scene, snapshot.Transform);
                host.SetBorder(options.ShowDebugBorder);
                host.Enable();
                host.Update(
                    snapshot.Transform.EditorBox,
                    snapshot.Transform.GetDpiMultiplier(),
                    snapshot.Transform.DpiSourceAvailable);
            });
        }
    }

    /// <inheritdoc />
    public void Hide()
    {
        lock (gate)
        {
            if (!disposed && windowThread is not null) InvokeWindow(host.Disable);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;

            disposed = true;
            if (windowThread is not null)
            {
                InvokeWindow(() =>
                {
                    host.Dispose();
                    WindowsNativeMethods.PostQuitMessage(0);
                });
                windowThread.Join();
            }
            else host.Dispose();
        }
    }

    // The caller holds gate. A native window must be used and destroyed on its
    // creating thread, which must also pump messages between dashboard updates.
    // The application loop uses async pool threads and Stop synchronously waits
    // for it, so dispatching to the Desktop UI thread would deadlock Stop.
    private void InvokeWindow(Action action)
    {
        if (windowThread is null)
        {
            windowThread = new Thread(RunWindowThread)
            {
                IsBackground = true,
                Name = "Geometry Dashboard overlay",
            };
            windowThread.Start();
        }

        uint threadId = windowThreadId.Task.GetAwaiter().GetResult();
        TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        windowCompletion = completion;
        Interlocked.Exchange(ref windowAction, action);
        if (!WindowsNativeMethods.PostThreadMessage(threadId, window_action_message, 0, 0))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not dispatch to the overlay thread.");

        completion.Task.GetAwaiter().GetResult();
    }

    private void RunWindowThread()
    {
        try
        {
            // Create the message queue before another thread posts work to it.
            WindowsNativeMethods.PeekMessage(out _, 0, 0, 0, 0);
            windowThreadId.SetResult(WindowsNativeMethods.GetCurrentThreadId());
            while (true)
            {
                int result = WindowsNativeMethods.GetMessage(out var message, 0, 0, 0);
                if (result == 0) break;
                if (result == -1) throw new Win32Exception(Marshal.GetLastWin32Error());

                if (message is { Window: 0, Id: window_action_message })
                {
                    var action = Interlocked.Exchange(ref windowAction, null);
                    var completion = windowCompletion!;
                    try
                    {
                        action!();
                        completion.SetResult();
                    }
                    catch (Exception exception)
                    {
                        completion.SetException(exception);
                    }
                }
                else
                {
                    WindowsNativeMethods.TranslateMessage(ref message);
                    WindowsNativeMethods.DispatchMessage(ref message);
                }
            }
        }
        catch (Exception exception)
        {
            windowThreadId.TrySetException(exception);
            windowCompletion?.TrySetException(exception);
        }
        finally
        {
            host.Dispose();
        }
    }
}
