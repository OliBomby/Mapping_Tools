using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tools.GeometryDashboard.Models;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.SnappingTools.Serialization;
using Mapping_Tools.Infrastructure.Editor;
using Mapping_Tools.Infrastructure.Files;
using Mapping_Tools.Infrastructure.Platform;
using Mapping_Tools.Infrastructure.Platform.GeometryDashboard;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Infrastructure.Tests.GeometryDashboard;

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
        sut.IsSupported.Should().BeFalse();
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task ReadGeometryDashboardAsync_WhenPlatformIsUnavailable_ReturnsNoSnapshot()
    {
        // Arrange
        WindowsEditorReaderAdapter sut = new(
            new ApplicationSettings(),
            new ApplicationDirectories(Path.Combine(Path.GetTempPath(), "Mapping Tools Tests")),
            () => false);

        // Act
        var result =
            await sut.ReadGeometryDashboardAsync(
                new GeometryDashboardProcess(7, new PlatformWindowId(42), "map.osu"));

        // Assert
        result.Should().BeNull();
        sut.Dispose();
    }

    [TestMethod]
    public void InputMethods_WhenPlatformIsUnavailable_ReturnFalseWithoutNativeCalls()
    {
        // Arrange
        WindowsGeometryDashboardInputService sut = new(() => false);
        Hotkey hotkey = new(56);

        // Act
        bool hotkeyDown = sut.IsHotkeyDown(hotkey);
        bool mouseDown = sut.IsMouseButtonDown(GeometryDashboardMouseButton.Left);
        bool cursorRead = sut.TryGetCursorPosition(out var position);
        bool cursorWrite = sut.TrySetCursorPosition(new Vector2(10, 20));

        // Assert
        sut.IsSupported.Should().BeFalse();
        hotkeyDown.Should().BeFalse();
        mouseDown.Should().BeFalse();
        cursorRead.Should().BeFalse();
        cursorWrite.Should().BeFalse();
        position.Should().Be(Vector2.Zero);
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
        sut.IsSupported.Should().BeFalse();
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
        sut.IsSupported.Should().BeFalse();
        result.Should().BeNull();
        windows.Should().BeEmpty();
    }

    [TestMethod]
    public void Create_WhenPlatformIsUnavailable_ReturnsSafeNoOpHost()
    {
        // Arrange
        WindowsGeometryDashboardWindowService windows = new(() => false);
        WindowsGeometryDashboardOverlayHostFactory factory = new(windows, () => false);

        // Act
        using var host = factory.Create();
        var act = () =>
        {
            host.Initialize(new PlatformWindowId(1));
            host.Enable();
            host.SetBorder(true);
            host.Update(new Box2(1, 2, 3, 4), new Vector2(1.5, 1.5), true);
            host.Invalidate();
            host.Disable();
        };

        // Assert
        host.IsSupported.Should().BeFalse();
        act.Should().NotThrow();
        host.IsVisible.Should().BeFalse();
        host.TargetWindow.Should().BeNull();
    }

    [TestMethod]
    public void Dispose_WhenCalledRepeatedlyAndAfterBorderChange_IsSafe()
    {
        // Arrange
        WindowsGeometryDashboardWindowService windows = new(() => false);
        using var host =
            new WindowsGeometryDashboardOverlayHostFactory(windows, () => false).Create();

        // Act
        host.Dispose();
        host.Dispose();
        var setBorder = () => host.SetBorder(true);

        // Assert
        setBorder.Should().NotThrow();
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
}
