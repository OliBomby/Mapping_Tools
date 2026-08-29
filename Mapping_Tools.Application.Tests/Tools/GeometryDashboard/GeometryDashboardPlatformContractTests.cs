using Mapping_Tools.Application.Tools.GeometryDashboard;
using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Tools.GeometryDashboard.Contracts;
using Mapping_Tools.Application.Tools.GeometryDashboard.Models;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Infrastructure.Tools.GeometryDashboard;
using Mapping_Tools.Infrastructure.Tools.GeometryDashboard.Contracts;
using Mapping_Tools.Infrastructure.Tools.GeometryDashboard.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.Tools.GeometryDashboard;

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
        HitObject hitObject = new() { Time = 1000 };
        LiveBeatmapSnapshot editor = new(
            @"C:\osu!\Songs\map.osu",
            [],
            [],
            [hitObject],
            0,
            1.4,
            1,
            9,
            4,
            1000,
            [hitObject]);
        FakeProcessDiscovery processes = new(process);
        FakeLiveBeatmapReader reader = new(editor);
        FakeWindowService windows = new(window);
        WindowsGeometryDashboardRuntimeService sut = new(processes, reader, windows);

        // Act
        var result = await sut.ReadAsync();

        // Assert
        result.Should().NotBeNull();
        result.Editor.Should().Be(editor);
        result.IsEditorActive.Should().BeTrue();
        processes.CallCount.Should().Be(1);
        reader.CallCount.Should().Be(1);
        windows.CallCount.Should().Be(1);
    }

    [TestMethod]
    public async Task ReadAsync_WhenProcessIsUnavailable_ReturnsNullWithoutReadingWindowOrEditor()
    {
        // Arrange
        FakeProcessDiscovery processes = new(null);
        FakeLiveBeatmapReader reader = new(null);
        FakeWindowService windows = new(null);
        WindowsGeometryDashboardRuntimeService sut = new(processes, reader, windows);

        // Act
        var result = await sut.ReadAsync();

        // Assert
        result.Should().BeNull();
        processes.CallCount.Should().Be(1);
        windows.CallCount.Should().Be(0);
        reader.CallCount.Should().Be(0);
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
        FakeLiveBeatmapReader reader = new(null);
        WindowsGeometryDashboardRuntimeService sut = new(
            new FakeProcessDiscovery(process),
            reader,
            new FakeWindowService(window));

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
        WindowsGeometryDashboardRuntimeService sut = new(
            new FakeProcessDiscovery(null),
            new FakeLiveBeatmapReader(null),
            new FakeWindowService(null));
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        // Act
        Func<Task> act = () => sut.ReadAsync(cancellation.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [TestMethod]
    public void LiveBeatmapSnapshot_WithMutableInput_CopiesHitObjectsAndDifficultyValues()
    {
        // Arrange
        List<HitObject> hitObjects = [new() { Time = 1000 }];

        // Act
        LiveBeatmapSnapshot snapshot = new(
            @"C:\osu!\Songs\map.osu",
            [],
            [],
            hitObjects,
            0,
            1.4,
            1,
            9,
            4,
            editorTime: 1000);
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

        public Task<GeometryDashboardProcess?> FindAsync(
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(process);
        }
    }

    private sealed class FakeLiveBeatmapReader(LiveBeatmapSnapshot? snapshot)
        : ILiveBeatmapReader
    {
        public int CallCount { get; private set; }

        public Task<LiveBeatmapSnapshot?> ReadAsync(
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(snapshot);
        }
    }

    private sealed class FakeWindowService(GeometryDashboardWindow? window)
        : IGeometryDashboardWindowService
    {
        public int CallCount { get; private set; }

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

}
