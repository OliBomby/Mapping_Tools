using System.Text.Json;
using System.Text.Json.Nodes;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tools.GeometryDashboard;
using Mapping_Tools.Application.Tools.GeometryDashboard.Contracts;
using Mapping_Tools.Application.Tools.GeometryDashboard.Models;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Settings.Models;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure;
using Mapping_Tools.Core.Tools.GeometryDashboard.Serialization;
using Mapping_Tools.Infrastructure.Projects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.Execution;

[TestClass]
public sealed class GeometryDashboardFixtureTests
{
    [TestMethod]
    public async Task SaveVirtualObjectsFixture_WithCapturedLiveMapState_ExportsMatchingObjects()
    {
        // Arrange
        string fixtureRoot = Path.Combine(AppContext.BaseDirectory, "Fixtures", "GeometryDashboard");
        using var record = JsonDocument.Parse(
            await File.ReadAllTextAsync(Path.Combine(fixtureRoot, "save-virtual-objects.json")));
        string beatmapPath = ResolveFixturePath(fixtureRoot, record.RootElement.GetProperty("sourceInput").GetString()!);
        string projectPath = ResolveFixturePath(fixtureRoot, record.RootElement.GetProperty("project").GetString()!);
        string expectedPath = ResolveFixturePath(fixtureRoot, record.RootElement.GetProperty("expectedOutput").GetString()!);
        Beatmap beatmap = new((await File.ReadAllLinesAsync(beatmapPath)).ToList());
        double[] visibleTimes = record.RootElement
            .GetProperty("visibleSourceObjects")
            .GetProperty("times")
            .EnumerateArray()
            .Select(value => value.GetDouble())
            .ToArray();
        var visibleHitObjects = beatmap.HitObjects
            .Where(hitObject => visibleTimes.Contains(hitObject.Time))
            .Select(hitObject => hitObject.DeepCopy())
            .ToArray();

        VersionedProjectJsonSerializer serializer = new();
        var persistedProject = serializer.Deserialize<GeometryDashboardEngineOptions>(
            await File.ReadAllTextAsync(projectPath));
        GeometryDashboardServiceOptions project = new();
        project.SetCurrentPreferences(persistedProject.CurrentPreferences);
        LiveBeatmapSnapshot liveBeatmap = new(
            beatmapPath,
            beatmap.Bookmarks,
            beatmap.BeatmapTiming.TimingPoints,
            visibleHitObjects,
            beatmap.General["PreviewTime"].IntValue,
            beatmap.BeatmapTiming.SliderMultiplier,
            beatmap.Difficulty["SliderTickRate"].DoubleValue,
            beatmap.Difficulty["ApproachRate"].DoubleValue,
            beatmap.Difficulty["CircleSize"].DoubleValue,
            record.RootElement.GetProperty("editorTime").GetDouble(),
            []);
        RecordingGeometryDashboardRuntime runtime = new(new GeometryDashboardRuntimeSnapshot(liveBeatmap, true));
        using GeometryDashboardService service = new(
            new ApplicationSettings { UseEditorReader = true },
            project,
            runtime,
            new SimulatedGeometryDashboardInput(),
            new SimulatedGeometryDashboardOverlay());

        var expectedObjects = serializer.Deserialize<RelevantObjectCollection>(
            await File.ReadAllTextAsync(expectedPath));
        foreach (var type in expectedObjects.Keys.Where(type => expectedObjects[type].Count == 0).ToArray())
            expectedObjects.Remove(type);
        string expectedJson = serializer.Serialize(expectedObjects);

        // Act
        await service.RefreshOnceAsync();
        service.ToggleLocked(GeometryDashboardTargetingMode.Enable);
        var actualObjects = service.GetLockedObjects();
        string actualJson = serializer.Serialize(actualObjects);

        // Assert
        visibleHitObjects.Should().HaveCount(
            record.RootElement.GetProperty("visibleSourceObjects").GetProperty("count").GetInt32());
        visibleHitObjects.Count(hitObject => hitObject.IsSlider).Should().Be(
            record.RootElement.GetProperty("visibleSourceObjects").GetProperty("sliders").GetInt32());
        visibleHitObjects.Count(hitObject => !hitObject.IsSlider).Should().Be(
            record.RootElement.GetProperty("visibleSourceObjects").GetProperty("circles").GetInt32());
        service.State.IsConnected.Should().BeTrue();
        service.State.DrawableCount.Should().Be(expectedObjects.GetCount());
        actualObjects.GetCount().Should().Be(expectedObjects.GetCount());
        actualObjects.Values
            .SelectMany(objects => objects)
            .Should()
            .AllSatisfy(relevantObject => relevantObject.IsLocked.Should().BeTrue());
        JsonNode.DeepEquals(JsonNode.Parse(expectedJson), JsonNode.Parse(actualJson)).Should().BeTrue();
    }

    private static string ResolveFixturePath(string fixtureRoot, string relativePath)
    {
        string path = Path.GetFullPath(Path.Combine(fixtureRoot, relativePath));
        File.Exists(path).Should().BeTrue($"Fixture path does not exist: {relativePath}");
        return path;
    }

    private sealed class RecordingGeometryDashboardRuntime(GeometryDashboardRuntimeSnapshot snapshot)
        : IGeometryDashboardRuntime
    {
        public bool IsProcessRunning => true;

        public Task<GeometryDashboardRuntimeSnapshot?> ReadAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<GeometryDashboardRuntimeSnapshot?>(snapshot);
        }
    }

    private sealed class SimulatedGeometryDashboardInput : IGeometryDashboardInputService
    {
        public bool IsSupported => true;

        public bool IsHotkeyDown(HotkeySettings? hotkey)
        {
            return false;
        }

        public bool IsMouseButtonDown(GeometryDashboardMouseButton button)
        {
            return false;
        }

        public bool TryGetCursorPosition(out Vector2 position)
        {
            position = Vector2.Zero;
            return false;
        }

        public bool TrySetCursorPosition(Vector2 position)
        {
            return false;
        }
    }

    private sealed class SimulatedGeometryDashboardOverlay : IGeometryDashboardOverlayService
    {
        public bool IsSupported => true;

        public bool IsVisible { get; private set; }

        public string? ConfigurationStatus => null;

        public void Update(GeometryDashboardOverlayScene scene, GeometryDashboardOverlayOptions options)
        {
            IsVisible = true;
        }

        public void Hide()
        {
            IsVisible = false;
        }

        public void Dispose() { }
    }
}
