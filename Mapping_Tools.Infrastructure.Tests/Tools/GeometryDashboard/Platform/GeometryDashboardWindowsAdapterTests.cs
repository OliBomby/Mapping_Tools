using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tools.GeometryDashboard.Contracts;
using Mapping_Tools.Application.Tools.GeometryDashboard.Models;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Settings.Models;
using Mapping_Tools.Core.Tools.GeometryDashboard.Serialization;
using Mapping_Tools.Infrastructure.Editor;
using Mapping_Tools.Infrastructure.Files;
using Mapping_Tools.Infrastructure.Platform;
using Mapping_Tools.Infrastructure.Tools.GeometryDashboard;
using Mapping_Tools.Infrastructure.Tools.GeometryDashboard.Contracts;
using Mapping_Tools.Infrastructure.Tools.GeometryDashboard.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Infrastructure.Tests.Tools.GeometryDashboard.Platform;

[TestClass]
public sealed class GeometryDashboardWindowsAdapterTests
{
    [TestMethod]
    public async Task FindAsync_WhenPlatformIsUnavailable_ReturnsNoProcess()
    {
        // Arrange
        WindowsOsuProcessDiscovery sut = new(() => false);

        // Act
        var result = await sut.FindAsync();

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task ReadAsync_WhenPlatformIsUnavailable_ReturnsNoSnapshot()
    {
        // Arrange
        WindowsEditorReaderAdapter sut = new(
            new ApplicationSettings(),
            new ApplicationDirectories(Path.Combine(Path.GetTempPath(), "Mapping Tools Tests")),
            () => false);

        // Act
        var result = await sut.ReadAsync();

        // Assert
        result.Should().BeNull();
        sut.Dispose();
    }

    [TestMethod]
    public void InputMethods_WhenPlatformIsUnavailable_ReturnFalseWithoutNativeCalls()
    {
        // Arrange
        WindowsGeometryDashboardInputService sut = new(() => false);
        HotkeySettings hotkey = new(56, 0);

        // Act
        bool hotkeyDown = sut.IsHotkeyDown(hotkey);
        bool mouseDown = sut.IsMouseButtonDown(GeometryDashboardMouseButton.Left);
        bool cursorRead = sut.TryGetCursorPosition(out var position);
        bool cursorWrite = sut.TrySetCursorPosition(new Vector2(10, 20));

        // Assert
        hotkeyDown.Should().BeFalse();
        mouseDown.Should().BeFalse();
        cursorRead.Should().BeFalse();
        cursorWrite.Should().BeFalse();
        position.Should().Be(Vector2.Zero);
    }

    [TestMethod]
    public void CoordinateTransform_EditorScreenRoundTrip_PreservesOsuCoordinate()
    {
        // Arrange
        var window = new GeometryDashboardWindow(
            new PlatformWindowId(42),
            7,
            "map.osu",
            new Box2(0, 0, 2560, 1440),
            true,
            true,
            Vector2.One,
            true);
        WindowsGeometryDashboardCoordinateTransform sut = new(
            window,
            new GeometryDashboardScreen(
                1,
                new Box2(0, 0, 2560, 1440),
                new Box2(0, 0, 2560, 1400),
                true,
                Vector2.One,
                true),
            new WindowsGeometryDashboardOsuDisplaySettings(
                new Vector2(2560, 1440),
                true,
                true,
                new Vector2(0.5, 0.5)),
            new Box2(0, 1, 0, 1));
        Vector2 editorCoordinate = new(256, 192);

        // Act
        var screenCoordinate = sut.EditorToScreenCoordinate(editorCoordinate);
        var roundTrip = sut.ScreenToEditorCoordinate(screenCoordinate);

        // Assert
        roundTrip.X.Should().BeApproximately(editorCoordinate.X, 0.000001);
        roundTrip.Y.Should().BeApproximately(editorCoordinate.Y, 0.000001);
    }

    [TestMethod]
    public void CoordinateContext_Refresh_ReplacesTransformWhenWindowMoves()
    {
        // Arrange
        string configPath = Path.Combine(Path.GetTempPath(), $"mapping-tools-geometry-{Guid.NewGuid():N}.cfg");
        File.WriteAllLines(configPath, ["Fullscreen=0", "Letterboxing=0", "Width=1280", "Height=720"]);
        GeometryDashboardProcess process = new(7, new PlatformWindowId(42), "map.osu");
        MutableWindowService windows = new(new GeometryDashboardWindow(
            process.MainWindow,
            process.ProcessId,
            process.MainWindowTitle,
            new Box2(0, 0, 1920, 1080),
            true,
            true,
            Vector2.One,
            true));
        WindowsGeometryDashboardCoordinateContext sut = new(
            new ApplicationSettings { OsuConfigPath = configPath },
            new PhysicalBeatmapsetFileSystem(),
            new FixedProcessDiscovery(process),
            windows,
            new FixedScreenService(new GeometryDashboardScreen(
                1,
                new Box2(0, 0, 1920, 1080),
                new Box2(0, 0, 1920, 1040),
                true,
                Vector2.One,
                true)),
            () => true);

        try
        {
            // Act
            sut.TryRefresh(new Box2(0, 1, 0, 1), out var first);
            windows.Window = windows.Window with { Bounds = new Box2(400, 100, 2320, 1180) };
            sut.TryRefresh(out var second);
            sut.TryGetCurrent(out var current);
            File.WriteAllLines(configPath, ["Fullscreen=0", "Letterboxing=0", "Width=1600", "Height=900"]);
            File.SetLastWriteTimeUtc(configPath, DateTime.UtcNow.AddSeconds(1));
            sut.TryRefresh(out var afterConfigurationChange);

            // Assert
            first.Transform.EditorToScreenCoordinate(Vector2.Zero).Should().NotBe(
                second.Transform.EditorToScreenCoordinate(Vector2.Zero));
            current.Should().Be(second);
            afterConfigurationChange.Transform.EditorResolution.X.Should().Be(1600);
        }
        finally
        {
            File.Delete(configPath);
        }
    }

    [TestMethod]
    public void CoordinateContext_Refresh_UsesMonitorContainingWindowForFullscreenLayout()
    {
        // Arrange
        string configPath = Path.Combine(Path.GetTempPath(), $"mapping-tools-geometry-{Guid.NewGuid():N}.cfg");
        File.WriteAllLines(configPath, ["Fullscreen=1", "Letterboxing=1", "WidthFullscreen=1920", "HeightFullscreen=1080"]);
        GeometryDashboardProcess process = new(7, new PlatformWindowId(42), "map.osu");
        GeometryDashboardWindow window = new(
            process.MainWindow,
            process.ProcessId,
            process.MainWindowTitle,
            new Box2(1920, 0, 3840, 1080),
            true,
            true,
            Vector2.One,
            true);
        GeometryDashboardScreen primary = new(
            1,
            new Box2(0, 0, 1920, 1080),
            new Box2(0, 0, 1920, 1040),
            true,
            Vector2.One,
            true);
        GeometryDashboardScreen windowScreen = new(
            2,
            new Box2(1920, 0, 3840, 1080),
            new Box2(1920, 0, 3840, 1040),
            false,
            Vector2.One,
            true);
        WindowsGeometryDashboardCoordinateContext sut = new(
            new ApplicationSettings { OsuConfigPath = configPath },
            new PhysicalBeatmapsetFileSystem(),
            new FixedProcessDiscovery(process),
            new MutableWindowService(window),
            new FixedScreenService(primary, windowScreen),
            () => true);

        try
        {
            // Act
            sut.TryRefresh(new Box2(0, 1, 0, 1), out var snapshot);

            // Assert
            snapshot.Transform.EditorToScreenCoordinate(Vector2.Zero).X.Should().BeGreaterThan(1900);
        }
        finally
        {
            File.Delete(configPath);
        }
    }

    [TestMethod]
    public void GetScreens_WhenPlatformIsUnavailable_ReturnsEmptyCollection()
    {
        // Arrange
        WindowsGeometryDashboardScreenService sut = new(() => false);

        // Act
        var screens = sut.GetScreens();
        var primary = sut.GetPrimaryScreen();
        var forWindow = sut.GetScreenForWindow(new PlatformWindowId(1));

        // Assert
        screens.Should().BeEmpty();
        primary.Should().BeNull();
        forWindow.Should().BeNull();
    }

    [TestMethod]
    public void GetWindow_WhenPlatformIsUnavailable_ReturnsNoWindow()
    {
        // Arrange
        WindowsGeometryDashboardWindowService sut = new(() => false);

        // Act
        var result = sut.GetWindow(new PlatformWindowId(1));
        var windows = sut.GetTopLevelWindows();

        // Assert
        result.Should().BeNull();
        windows.Should().BeEmpty();
    }

    [TestMethod]
    public void OverlayService_WhenPlatformIsUnavailable_IsSafeNoOp()
    {
        // Arrange
        WindowsGeometryDashboardWindowService windows = new(() => false);
        WindowsOsuProcessDiscovery processes = new(() => false);
        WindowsGeometryDashboardScreenService screens = new(() => false);
        WindowsGeometryDashboardCoordinateContext coordinates = new(
            new ApplicationSettings(),
            new EmptyTextFileStore(),
            processes,
            windows,
            screens,
            () => false);

        // Act
        using var host = new WindowsGeometryDashboardOverlayService(coordinates, windows, () => false);
        var act = () =>
        {
            host.Update(
                GeometryDashboardOverlayScene.Empty,
                new GeometryDashboardOverlayOptions(new Box2(0, 1, 0, 1), true));
            host.Hide();
        };

        // Assert
        act.Should().NotThrow();
        host.IsVisible.Should().BeFalse();
    }

    [TestMethod]
    public void Dispose_WhenCalledRepeatedlyAndAfterBorderChange_IsSafe()
    {
        // Arrange
        WindowsGeometryDashboardWindowService windows = new(() => false);
        WindowsOsuProcessDiscovery processes = new(() => false);
        WindowsGeometryDashboardScreenService screens = new(() => false);
        WindowsGeometryDashboardCoordinateContext coordinates = new(
            new ApplicationSettings(),
            new EmptyTextFileStore(),
            processes,
            windows,
            screens,
            () => false);
        using var host = new WindowsGeometryDashboardOverlayService(coordinates, windows, () => false);

        // Act
        host.Dispose();
        host.Dispose();
        var hide = () => host.Hide();

        // Assert
        hide.Should().NotThrow();
    }

    [TestMethod]
    public void OverlayBounds_WithLiveDpi_PreservesLegacyScaleOffsetAndRounding()
    {
        // Arrange
        Box2 physicalBounds = new(-1920, 100, 0, 1100);

        // Act
        bool converted = WindowsGeometryDashboardOverlayHost.TryConvertBounds(
            physicalBounds,
            new Vector2(2, 2),
            true,
            out var nativeBounds);

        // Assert
        converted.Should().BeTrue();
        nativeBounds.Should().Be(
            new WindowsGeometryDashboardOverlayHost.NativeBounds(-960, 50, 960, 500));
    }

    [TestMethod]
    public void OverlayBounds_WithUnavailableDpiSource_UsesPhysicalCoordinates()
    {
        // Arrange
        Box2 physicalBounds = new(-1920, 100, 0, 1100);

        // Act
        bool converted = WindowsGeometryDashboardOverlayHost.TryConvertBounds(
            physicalBounds,
            Vector2.Zero,
            false,
            out var nativeBounds);

        // Assert
        converted.Should().BeTrue();
        nativeBounds.Should().Be(
            new WindowsGeometryDashboardOverlayHost.NativeBounds(-1920, 100, 1920, 1000));
    }

    [TestMethod]
    public void OverlayBounds_WithInvalidDpiOrCoordinates_IsRejected()
    {
        // Arrange
        Box2 validBounds = new(0, 0, 100, 100);

        // Act
        bool invalidDpi = WindowsGeometryDashboardOverlayHost.TryConvertBounds(
            validBounds,
            new Vector2(0, 1),
            true,
            out _);
        bool invalidCoordinates = WindowsGeometryDashboardOverlayHost.TryConvertBounds(
            new Box2(double.NaN, 0, 100, 100),
            Vector2.One,
            false,
            out _);

        // Assert
        invalidDpi.Should().BeFalse();
        invalidCoordinates.Should().BeFalse();
    }

    [TestMethod]
    public void Start_WhenPlatformIsUnavailable_DoesNotInvokeNativeHook()
    {
        // Arrange
        WindowsGlobalHotkeyService sut = new(() => false);
        sut.SetBinding(
            "geometry",
            new HotkeySettings(56, 0),
            _ => Task.CompletedTask);

        // Act
        var act = () =>
        {
            sut.Start();
            sut.Stop();
        };

        // Assert
        act.Should().NotThrow();
    }

    [TestMethod]
    public void ConvertLegacyKeyToVirtualKey_PreservesPersistedWpfKeyValues()
    {
        // Arrange
        int[] legacyKeys = [44, 41, 77, 101, 116, 121, 122, 132, 141, 155];
        int[] expectedVirtualKeys = [65, 55, 99, 123, 160, 165, 166, 176, 187, 229];

        // Act
        int[] actualVirtualKeys = legacyKeys
            .Select(WindowsGlobalHotkeyService.ConvertLegacyKeyToVirtualKey)
            .ToArray();

        // Assert
        actualVirtualKeys.Should().Equal(expectedVirtualKeys);
    }

    [TestMethod]
    public void ConvertLegacyKeyToVirtualKey_WithUnsupportedPersistedValue_Throws()
    {
        // Arrange
        Action act = () => WindowsGlobalHotkeyService.ConvertLegacyKeyToVirtualKey(156);

        // Act
        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    private sealed class EmptyTextFileStore : Mapping_Tools.Application.Abstractions.ITextFileStore
    {
        public IReadOnlyList<string> ReadAllLines(string path) => [];
        public void WriteAllLines(string path, IEnumerable<string> lines) { }
        public void Delete(string path) { }
        public string GetParentFolder(string path) => string.Empty;
        public string CombinePath(string parent, string child) => child;
    }

    private sealed class FixedProcessDiscovery(GeometryDashboardProcess process) : IGeometryDashboardProcessDiscovery
    {
        public Task<GeometryDashboardProcess?> FindAsync(CancellationToken cancellationToken = default) => Task.FromResult<GeometryDashboardProcess?>(process);
    }

    private sealed class MutableWindowService(GeometryDashboardWindow window) : IGeometryDashboardWindowService
    {
        public GeometryDashboardWindow Window { get; set; } = window;
        public GeometryDashboardWindow? GetWindow(PlatformWindowId window) => Window.Id == window ? Window : null;
        public GeometryDashboardWindow? GetMainWindow(GeometryDashboardProcess process) => Window;
        public IReadOnlyList<GeometryDashboardWindow> GetTopLevelWindows() => [Window];
    }

    private sealed class FixedScreenService(
        GeometryDashboardScreen screen,
        GeometryDashboardScreen? windowScreen = null) : IGeometryDashboardScreenService
    {
        public IReadOnlyList<GeometryDashboardScreen> GetScreens() => [screen];
        public GeometryDashboardScreen? GetPrimaryScreen() => screen;
        public GeometryDashboardScreen? GetScreenForWindow(PlatformWindowId window) => windowScreen ?? screen;
    }
}
