using Mapping_Tools.Application.GeometryDashboard;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.MathUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.GeometryDashboard;

[TestClass]
public sealed class GeometryDashboardPlatformContractTests
{
    [TestMethod]
    public async Task ReadAsync_WithAllPlatformPortsAvailable_ReturnsCompleteSnapshot()
    {
        // Arrange
        GeometryDashboardProcess process = new(
            7,
            new PlatformWindowId(42),
            "map.osu");
        GeometryDashboardWindow window = new(
            process.MainWindow,
            process.ProcessId,
            process.MainWindowTitle,
            new Box2(0, 0, 1920, 1080),
            true,
            true,
            Vector2.One,
            true);
        GeometryDashboardEditorSnapshot editor = new(
            @"C:\osu!\Songs\map.osu",
            9,
            4,
            1000,
            [new HitObject { Time = 1000 }]);
        GeometryDashboardScreen screen = new(
            1,
            new Box2(0, 0, 1920, 1080),
            new Box2(0, 0, 1920, 1040),
            true,
            Vector2.One,
            true);
        FakeProcessDiscovery processes = new(process);
        FakeEditorReader reader = new(editor);
        FakeWindowService windows = new(window);
        FakeScreenService screens = new(screen);
        GeometryDashboardRuntimeService sut = new(processes, reader, windows, screens);

        // Act
        var result = await sut.ReadAsync();

        // Assert
        result.Should().NotBeNull();
        result!.Process.Should().Be(process);
        result.Window.Should().Be(window);
        result.Editor.Should().Be(editor);
        result.PrimaryScreen.Should().Be(screen);
        processes.CallCount.Should().Be(1);
        reader.CallCount.Should().Be(1);
        reader.LastProcess.Should().Be(process);
        windows.CallCount.Should().Be(1);
        screens.CallCount.Should().Be(1);
    }

    [TestMethod]
    public async Task ReadAsync_WhenProcessIsUnavailable_ReturnsNullWithoutReadingWindowOrEditor()
    {
        // Arrange
        FakeProcessDiscovery processes = new(null);
        FakeEditorReader reader = new(null);
        FakeWindowService windows = new(null);
        FakeScreenService screens = new(null);
        GeometryDashboardRuntimeService sut = new(processes, reader, windows, screens);

        // Act
        var result = await sut.ReadAsync();

        // Assert
        result.Should().BeNull();
        processes.CallCount.Should().Be(1);
        windows.CallCount.Should().Be(0);
        reader.CallCount.Should().Be(0);
        screens.CallCount.Should().Be(0);
    }

    [TestMethod]
    public async Task ReadAsync_WhenWindowIsNotAnOpenEditor_ReturnsNullWithoutReadingEditor()
    {
        // Arrange
        GeometryDashboardProcess process = new(
            7,
            new PlatformWindowId(42),
            "osu!");
        GeometryDashboardWindow window = new(
            process.MainWindow,
            process.ProcessId,
            process.MainWindowTitle,
            new Box2(0, 0, 1920, 1080),
            true,
            true,
            Vector2.One,
            true);
        FakeEditorReader reader = new(null);
        GeometryDashboardRuntimeService sut = new(
            new FakeProcessDiscovery(process),
            reader,
            new FakeWindowService(window),
            new FakeScreenService(null));

        // Act
        var result = await sut.ReadAsync();

        // Assert
        result.Should().BeNull();
        reader.CallCount.Should().Be(0);
    }

    [TestMethod]
    public async Task ReadAsync_WhenCancellationIsRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        GeometryDashboardRuntimeService sut = new(
            new FakeProcessDiscovery(null),
            new FakeEditorReader(null),
            new FakeWindowService(null),
            new FakeScreenService(null));
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        // Act
        Func<Task> act = () => sut.ReadAsync(cancellation.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [TestMethod]
    public void GeometryDashboardEditorSnapshot_WithMutableInput_CopiesHitObjects()
    {
        // Arrange
        List<HitObject> hitObjects = [new() { Time = 1000 }];

        // Act
        GeometryDashboardEditorSnapshot snapshot = new(
            @"C:\osu!\Songs\map.osu",
            9,
            4,
            1000,
            hitObjects);
        hitObjects.Clear();

        // Assert
        snapshot.Path.Should().Be(@"C:\osu!\Songs\map.osu");
        snapshot.ApproachRate.Should().Be(9);
        snapshot.CircleSize.Should().Be(4);
        snapshot.EditorTime.Should().Be(1000);
        snapshot.HitObjects.Should().ContainSingle();
        snapshot.HitObjects[0].Time.Should().Be(1000);
    }

    [TestMethod]
    public void PlatformWindowId_WithZeroValue_IsEmpty()
    {
        // Arrange
        PlatformWindowId sut = new(0);

        // Act
        bool result = sut.IsEmpty;

        // Assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public void GeometryDashboardWindow_WithPhysicalBounds_PreservesCoordinatesAndDpi()
    {
        // Arrange
        Box2 bounds = new(-1920, 0, 0, 1080);

        // Act
        GeometryDashboardWindow window = new(
            new PlatformWindowId(42),
            7,
            "osu!",
            bounds,
            true,
            true,
            new Vector2(1.5, 1.25),
            true);

        // Assert
        window.Bounds.Should().Be(bounds);
        window.DpiScale.Should().Be(new Vector2(1.5, 1.25));
        window.DpiSourceAvailable.Should().BeTrue();
    }

    private sealed class FakeProcessDiscovery(GeometryDashboardProcess? process)
        : IGeometryDashboardProcessDiscovery
    {
        public int CallCount { get; private set; }

        public bool IsSupported => true;

        public Task<GeometryDashboardProcess?> FindAsync(
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(process);
        }
    }

    private sealed class FakeEditorReader(GeometryDashboardEditorSnapshot? snapshot)
        : IGeometryDashboardEditorReader
    {
        public int CallCount { get; private set; }

        public GeometryDashboardProcess? LastProcess { get; private set; }

        public Task<GeometryDashboardEditorSnapshot?> ReadGeometryDashboardAsync(
            GeometryDashboardProcess process,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastProcess = process;
            return Task.FromResult(snapshot);
        }
    }

    private sealed class FakeWindowService(GeometryDashboardWindow? window)
        : IGeometryDashboardWindowService
    {
        public int CallCount { get; private set; }

        public bool IsSupported => true;

        public GeometryDashboardWindow? GetWindow(PlatformWindowId windowId)
        {
            throw new NotSupportedException();
        }

        public GeometryDashboardWindow? GetMainWindow(GeometryDashboardProcess process)
        {
            CallCount++;
            return window;
        }

        public IReadOnlyList<GeometryDashboardWindow> GetTopLevelWindows()
        {
            return [];
        }
    }

    private sealed class FakeScreenService(GeometryDashboardScreen? screen)
        : IGeometryDashboardScreenService
    {
        public int CallCount { get; private set; }

        public bool IsSupported => true;

        public IReadOnlyList<GeometryDashboardScreen> GetScreens()
        {
            return [];
        }

        public GeometryDashboardScreen? GetPrimaryScreen()
        {
            CallCount++;
            return screen;
        }

        public GeometryDashboardScreen? GetScreenForWindow(PlatformWindowId window)
        {
            throw new NotSupportedException();
        }
    }
}
