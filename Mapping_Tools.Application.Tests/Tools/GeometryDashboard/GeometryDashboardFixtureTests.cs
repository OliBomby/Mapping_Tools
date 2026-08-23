using System.Text.Json;
using Mapping_Tools.Core.BeatmapHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.Tools.GeometryDashboard;

[TestClass]
public sealed class GeometryDashboardFixtureTests
{
    [TestMethod]
    public void LoadSaveVirtualObjectsFixture_ReferencesExistingInputsAndExpectedExport()
    {
        // Arrange
        string fixtureRoot = Path.Combine(AppContext.BaseDirectory, "Fixtures", "GeometryDashboard");
        using var record = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(fixtureRoot, "save-virtual-objects.json")));
        string sourcePath = ResolveFixturePath(fixtureRoot, record.RootElement.GetProperty("sourceInput").GetString()!);
        string projectPath = ResolveFixturePath(fixtureRoot, record.RootElement.GetProperty("project").GetString()!);
        string expectedPath = ResolveFixturePath(fixtureRoot, record.RootElement.GetProperty("expectedOutput").GetString()!);

        // Act
        Beatmap source = new(File.ReadAllLines(sourcePath).ToList());
        using var project = JsonDocument.Parse(File.ReadAllText(projectPath));
        using var expected = JsonDocument.Parse(File.ReadAllText(expectedPath));
        string report = File.ReadAllText(Path.Combine(fixtureRoot, "save-virtual-objects-report.md"));

        // Assert
        source.HitObjects.Should().NotBeEmpty();
        project.RootElement.ValueKind.Should().Be(JsonValueKind.Object);
        expected.RootElement.ValueKind.Should().Be(JsonValueKind.Object);
        expected.RootElement.EnumerateObject().Should().NotBeEmpty();
        report.Should().NotBeNullOrWhiteSpace();
    }

    private static string ResolveFixturePath(string fixtureRoot, string relativePath)
    {
        string path = Path.GetFullPath(Path.Combine(fixtureRoot, relativePath));
        File.Exists(path).Should().BeTrue($"Fixture path does not exist: {relativePath}");
        return path;
    }
}
