using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Diagnostics;
using Mapping_Tools.Desktop.Localization;

namespace Mapping_Tools.Desktop.Tests.TestHelpers;

/// <summary>Hosts a real Avalonia control in a finite headless window for input tests.</summary>
internal sealed class HeadlessViewHost : IDisposable
{
    private readonly Window window;

    internal Window Window => window;

    private HeadlessViewHost(Window window)
    {
        this.window = window;
    }

    internal static HeadlessViewHost Show(Control view, double width = 900, double height = 700)
    {
        Window window = new()
        {
            Width = width,
            Height = height,
            Content = view,
        };
        window.Show();
        RunDispatcherJobs();
        return new HeadlessViewHost(window);
    }

    internal static HeadlessViewHost ShowWindow(Window window)
    {
        window.Show();
        RunDispatcherJobs();
        return new HeadlessViewHost(window);
    }

    internal static HeadlessViewHost Attach(Window window)
    {
        return new HeadlessViewHost(window);
    }

    internal T Find<T>(string name) where T : Control
    {
        return window.GetVisualDescendants().OfType<T>().FirstOrDefault(control => control.Name == name)
               ?? throw new InvalidOperationException($"Could not find control '{name}'.");
    }

    internal void Click(Control control)
    {
        Click(control, KeyModifiers.None);
    }

    internal void Click(Control control, KeyModifiers modifiers)
    {
        Point localPoint = new(control.Bounds.Width / 2, control.Bounds.Height / 2);
        Click(control, localPoint, modifiers);
    }

    internal void Click(Control control, Point localPoint)
    {
        Click(control, localPoint, KeyModifiers.None);
    }

    private void Click(Control control, Point localPoint, KeyModifiers modifiers)
    {
        Point point = control.TranslatePoint(localPoint, window)
                      ?? throw new InvalidOperationException("Could not locate the control inside its host window.");
        RawInputModifiers rawModifiers = RawInputModifiers.LeftMouseButton;
        if (modifiers.HasFlag(KeyModifiers.Shift)) rawModifiers |= RawInputModifiers.Shift;
        if (modifiers.HasFlag(KeyModifiers.Control)) rawModifiers |= RawInputModifiers.Control;
        if (modifiers.HasFlag(KeyModifiers.Alt)) rawModifiers |= RawInputModifiers.Alt;
        if (modifiers.HasFlag(KeyModifiers.Meta)) rawModifiers |= RawInputModifiers.Meta;

        window.MouseMove(point);
        window.MouseDown(point, MouseButton.Left, rawModifiers);
        window.MouseUp(point, MouseButton.Left, rawModifiers & ~RawInputModifiers.LeftMouseButton);
        RunDispatcherJobs();
    }

    internal void DoubleClick(Control control)
    {
        Point localPoint = new(control.Bounds.Width / 2, control.Bounds.Height / 2);
        Point point = control.TranslatePoint(localPoint, window)
                      ?? throw new InvalidOperationException("Could not locate the control inside its host window.");
        window.MouseMove(point);
        window.MouseDown(point, MouseButton.Left, RawInputModifiers.LeftMouseButton);
        window.MouseUp(point, MouseButton.Left);
        window.MouseDown(point, MouseButton.Left, RawInputModifiers.LeftMouseButton);
        window.MouseUp(point, MouseButton.Left);
        RunDispatcherJobs();
    }

    internal void Drag(Control control, Point start, Point end)
    {
        Point startPoint = control.TranslatePoint(start, window)
                           ?? throw new InvalidOperationException("Could not locate the drag start inside the host window.");
        Point endPoint = control.TranslatePoint(end, window)
                         ?? throw new InvalidOperationException("Could not locate the drag end inside the host window.");
        window.MouseMove(startPoint);
        window.MouseDown(startPoint, MouseButton.Left, RawInputModifiers.LeftMouseButton);
        window.MouseMove(endPoint, RawInputModifiers.LeftMouseButton);
        window.MouseUp(endPoint, MouseButton.Left);
        RunDispatcherJobs();
    }

    internal void TypeText(string text)
    {
        window.KeyTextInput(text);
        RunDispatcherJobs();
    }

    internal void PressKey(Key key, RawInputModifiers modifiers, PhysicalKey physicalKey, string keySymbol)
    {
        window.KeyPress(key, modifiers, physicalKey, keySymbol);
        RunDispatcherJobs();
    }

    internal static Task DrainAsync(Func<bool> completed, TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(completed);
        TimeSpan maximumWait = timeout ?? TimeSpan.FromSeconds(5);
        if (maximumWait <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout), DesktopStrings.Test_HeadlessViewTimeoutMustBePositive);

        Stopwatch elapsed = Stopwatch.StartNew();
        while (true)
        {
            RunDispatcherJobs();
            if (completed()) return Task.CompletedTask;
            if (elapsed.Elapsed >= maximumWait)
                throw new TimeoutException($"Headless UI work did not complete within {maximumWait}.");

            Thread.Yield();
        }
    }

    internal static void PumpDispatcherUntil(Func<bool> completed, TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(completed);
        TimeSpan maximumWait = timeout ?? TimeSpan.FromSeconds(5);
        if (maximumWait <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout), DesktopStrings.Test_HeadlessViewTimeoutMustBePositive);

        CancellationTokenSource cancellation = new(maximumWait);
        DispatcherTimer timer = new(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(10),
        };
        DispatcherWait dispatcherWait = new(completed, cancellation);
        SynchronizationContext? synchronizationContext = SynchronizationContext.Current;
        timer.Tick += dispatcherWait.OnTick;
        timer.Start();
        try
        {
            Dispatcher.UIThread.MainLoop(cancellation.Token);
        }
        catch (OperationCanceledException) when (completed())
        {
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            throw new TimeoutException($"Headless UI work did not complete within {maximumWait}.");
        }
        finally
        {
            timer.Tick -= dispatcherWait.OnTick;
            timer.Stop();
            cancellation.Dispose();
            SynchronizationContext.SetSynchronizationContext(synchronizationContext);
        }

        if (!completed())
            throw new TimeoutException($"Headless UI work did not complete within {maximumWait}.");
    }

    public void Dispose()
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            window.Close();
            RunDispatcherJobs();
            return;
        }

        Dispatcher.UIThread.InvokeAsync(() =>
        {
            window.Close();
            RunDispatcherJobs();
        }).GetAwaiter().GetResult();
    }

    internal static void RunDispatcherJobs()
    {
        SynchronizationContext? synchronizationContext = SynchronizationContext.Current;
        try
        {
            Dispatcher.UIThread.RunJobs();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(synchronizationContext);
        }
    }

    private sealed class DispatcherWait(Func<bool> completed, CancellationTokenSource cancellation)
    {
        public void OnTick(object? sender, EventArgs eventArgs)
        {
            if (completed()) cancellation.Cancel();
        }
    }
}
