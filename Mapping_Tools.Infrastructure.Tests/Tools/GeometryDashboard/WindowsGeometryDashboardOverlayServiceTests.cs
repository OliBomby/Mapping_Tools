using System.Runtime.InteropServices;
using System.Text;
using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tools.GeometryDashboard.Models;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Infrastructure.Platform;
using Mapping_Tools.Infrastructure.Tools.GeometryDashboard;
using Mapping_Tools.Infrastructure.Tools.GeometryDashboard.Contracts;
using Mapping_Tools.Infrastructure.Tools.GeometryDashboard.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Infrastructure.Tests.Tools.GeometryDashboard;

[TestClass]
[DoNotParallelize]
public sealed class WindowsGeometryDashboardOverlayServiceTests
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    [Timeout(15000)]
    public async Task Update_AfterProcessRestart_RecreatesResponsiveTopmostWindow(bool reuseTargetHandle)
    {
        // Arrange
        if (!OperatingSystem.IsWindows()) Assert.Inconclusive("Requires native Windows windows.");
        Target target = new();
        target.Window = target.Window with { IsActivated = false };
        using var service = CreateService(target);
        await OnNewThread(() => Update(service));
        nint original = FindOverlay();
        bool initiallyTopmost = (GetWindowLong(original, -20) & 0x8) != 0;
        uint ownerThread = WindowsNativeMethods.GetWindowThreadProcessId(original, out _);
        target.Window = target.Window with { IsActivated = true };
        await OnNewThread(() => Update(service));

        // Act
        await OnNewThread(service.Hide);
        bool hidden = !WindowsNativeMethods.IsWindowVisible(original);
        target.Window = target.Window with
        {
            Id = reuseTargetHandle ? target.Window.Id : new PlatformWindowId(200),
            ProcessId = 2,
            IsActivated = false,
        };
        await OnNewThread(() => Update(service));
        nint replacement = FindOverlay();
        bool originalDestroyed = !WindowsNativeMethods.IsWindow(original);
        // No positioning/show call has run for this HWND yet. Restart recovery
        // must establish topmost at creation, even while Mapping Tools is inactive.
        bool replacementInitiallyTopmost = (GetWindowLong(replacement, -20) & 0x8) != 0;
        target.Window = target.Window with { IsActivated = true };
        await OnNewThread(() => Update(service));
        bool visible = WindowsNativeMethods.IsWindowVisible(replacement);
        bool topmost = (GetWindowLong(replacement, -20) & 0x8) != 0;
        uint replacementThread = WindowsNativeMethods.GetWindowThreadProcessId(replacement, out _);
        // WM_NULL must be serviced while no dashboard update is running.
        bool responds = SendMessageTimeout(replacement, 0, 0, 0, 0x2, 1000, out _) != 0;
        await OnNewThread(() => Update(service));
        nint afterUpdate = FindOverlay();
        await OnNewThread(service.Dispose);

        // Assert
        original.Should().NotBe(0);
        initiallyTopmost.Should().BeTrue();
        replacementInitiallyTopmost.Should().BeTrue();
        hidden.Should().BeTrue();
        originalDestroyed.Should().BeTrue();
        replacement.Should().NotBe(0).And.NotBe(original);
        replacementThread.Should().Be(ownerThread);
        visible.Should().BeTrue();
        topmost.Should().BeTrue();
        responds.Should().BeTrue();
        afterUpdate.Should().Be(replacement, "ordinary updates should keep the existing native window");
        WindowsNativeMethods.IsWindow(replacement).Should().BeFalse();
        FindOverlay().Should().Be(0, "disposal must leave no orphan overlay windows");
    }

    [TestMethod]
    [Timeout(15000)]
    public async Task Update_AfterNativeWindowIsDestroyed_RecreatesWindowForSameTarget()
    {
        // Arrange
        if (!OperatingSystem.IsWindows()) Assert.Inconclusive("Requires native Windows windows.");
        Target target = new();
        using var service = CreateService(target);
        await OnNewThread(() => Update(service));
        nint original = FindOverlay();

        // Act
        // WM_CLOSE lets the owner thread destroy the HWND through DefWindowProc.
        bool closed = SendMessageTimeout(original, 0x10, 0, 0, 0x2, 1000, out _) != 0;
        bool destroyed = !WindowsNativeMethods.IsWindow(original);
        await OnNewThread(() => Update(service));
        nint replacement = FindOverlay();

        // Assert
        original.Should().NotBe(0);
        closed.Should().BeTrue();
        destroyed.Should().BeTrue();
        replacement.Should().NotBe(0).And.NotBe(original);
        service.IsVisible.Should().BeTrue();
    }

    private static Task OnNewThread(Action action) =>
        Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

    private static void Update(WindowsGeometryDashboardOverlayService service) =>
        service.Update(GeometryDashboardOverlayScene.Empty, new GeometryDashboardOverlayOptions(new Box2(), false));

    private static WindowsGeometryDashboardOverlayService CreateService(Target target)
    {
        WindowsGeometryDashboardCoordinateContext coordinates = new(
            new ApplicationSettings { OsuConfigPath = "overlay-test.cfg" },
            new ConfigStore(),
            target,
            target,
            new WindowsGeometryDashboardScreenService(() => false));
        return new WindowsGeometryDashboardOverlayService(coordinates, target);
    }

    private static nint FindOverlay()
    {
        List<nint> overlays = [];
        WindowsNativeMethods.EnumWindows((window, _) =>
        {
            WindowsNativeMethods.GetWindowThreadProcessId(window, out uint processId);
            if (processId != Environment.ProcessId) return true;
            StringBuilder name = new(256);
            GetClassName(window, name, name.Capacity);
            if (name.ToString() == "MappingTools.GeometryDashboardOverlayWindow") overlays.Add(window);
            return true;
        }, 0);
        return overlays.SingleOrDefault();
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(nint window, StringBuilder name, int capacity);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowLong(nint window, int index);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint SendMessageTimeout(nint window, uint message, nint wParam, nint lParam, uint flags, uint timeout, out nuint result);

    private sealed class Target : IGeometryDashboardProcessDiscovery, IGeometryDashboardWindowService
    {
        public GeometryDashboardWindow Window { get; set; } = new(
            new PlatformWindowId(100), 1, "test.osu", new Box2(0, 0, 320, 240), true, true, Vector2.One, false);

        public Task<GeometryDashboardProcess?> FindAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<GeometryDashboardProcess?>(new(Window.ProcessId, Window.Id, Window.Title));

        public GeometryDashboardWindow? GetWindow(PlatformWindowId window) => window == Window.Id ? Window : null;
        public GeometryDashboardWindow? GetMainWindow(GeometryDashboardProcess process) => Window;
        public IReadOnlyList<GeometryDashboardWindow> GetTopLevelWindows() => [Window];
    }

    private sealed class ConfigStore : ITextFileStore
    {
        public IReadOnlyList<string> ReadAllLines(string path) => ["Fullscreen = false", "Letterboxing = false", "Width = 320", "Height = 240"];
        public void WriteAllLines(string path, IEnumerable<string> lines) => throw new NotSupportedException();
        public void Delete(string path) => throw new NotSupportedException();
        public string GetParentFolder(string path) => string.Empty;
        public string CombinePath(string parent, string child) => Path.Combine(parent, child);
    }
}
