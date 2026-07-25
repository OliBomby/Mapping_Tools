using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests;

[TestClass]
public sealed class WaveZeroFixtureTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();
    private static readonly string ManifestPath = Path.Combine(RepositoryRoot, "tests", "fixtures", "wave0", "manifest.json");

    [TestMethod]
    public void LoadManifest_RequiredFixtureGroups_ContainsAll()
    {
        // Arrange
        var manifest = LoadManifest();
        string[] requiredGroups =
        [
            "beatmap", "storyboard", "transformation", "project", "pattern",
            "audio", "mapset", "settings", "platform-failure", "geometry-dashboard"
        ];

        // Act
        var actualGroups = manifest.Fixtures.Select(fixture => fixture.Group).ToHashSet(StringComparer.Ordinal);
        var missing = requiredGroups.Where(group => !actualGroups.Contains(group)).ToArray();

        // Assert
        missing.Length.Should().Be(0, $"Missing fixture groups: {string.Join(", ", missing)}");
    }

    [TestMethod]
    public void LoadManifest_FixtureIdsAndPaths_AreUnique()
    {
        // Arrange
        // Act
        var fixtures = LoadManifest().Fixtures;
        var duplicateIds = fixtures.GroupBy(fixture => fixture.Id, StringComparer.Ordinal)
            .Where(group => group.Count() > 1).Select(group => group.Key).ToArray();
        var duplicatePaths = fixtures.GroupBy(fixture => fixture.Path, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1).Select(group => group.Key).ToArray();

        // Assert
        duplicateIds.Length.Should().Be(0, $"Duplicate fixture IDs: {string.Join(", ", duplicateIds)}");
        duplicatePaths.Length.Should().Be(0, $"Duplicate fixture paths: {string.Join(", ", duplicatePaths)}");
    }

    [TestMethod]
    public void LoadManifest_VersionedFixtures_MatchRecordedHashes()
    {
        // Arrange
        var failures = new List<string>();

        foreach (var fixture in LoadManifest().Fixtures.Where(fixture => fixture.Sha256 is not null))
        {
            var path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(ManifestPath)!, fixture.Path));
            if (!File.Exists(path))
            {
                failures.Add($"{fixture.Id}: missing {Path.GetRelativePath(RepositoryRoot, path)}");
                continue;
            }

            // Act
            var actualHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
            if (!actualHash.Equals(fixture.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                failures.Add($"{fixture.Id}: expected {fixture.Sha256}, actual {actualHash}");
            // Assert
            }
        }

        failures.Count.Should().Be(0, string.Join(Environment.NewLine, failures));
    }

    [TestMethod]
    public void LoadManifest_DestructiveFeatures_HaveBaselineRecords()
    {
        // Arrange
        // Act
        var manifest = LoadManifest();
        var baselineIds = manifest.Fixtures
            .Where(fixture => fixture.Group == "transformation")
            .Select(fixture => fixture.Id)
            .ToHashSet(StringComparer.Ordinal);
        var missing = manifest.DestructiveFeatures
            .Where(feature => !baselineIds.Contains(feature.BaselineFixtureId))
            .Select(feature => feature.Id)
            .ToArray();

        // Assert
        missing.Length.Should().Be(0, $"Features without baseline records: {string.Join(", ", missing)}");
    }

    [TestMethod]
    public void LoadManifest_BaselineRecords_ReferenceVersionedFiles()
    {
        // Arrange
        var failures = new List<string>();
        var manifest = LoadManifest();
        var baselineIds = manifest.DestructiveFeatures
            .Select(feature => feature.BaselineFixtureId)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var fixture in manifest.Fixtures.Where(fixture => baselineIds.Contains(fixture.Id)))
        {
            var recordPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(ManifestPath)!, fixture.Path));
            var record = JsonSerializer.Deserialize<BaselineRecord>(File.ReadAllText(recordPath), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new InvalidDataException($"Baseline record is empty: {fixture.Path}");

            VerifyRelativeFile(recordPath, record.SeedInput, $"{fixture.Id} seed", failures);
            if (record.Options is not null)
            {
                VerifyRelativeFile(recordPath, record.Options, $"{fixture.Id} options", failures);
            }

            if (record.ExpectedOutput is not null)
            {
                VerifyRelativeFile(recordPath, record.ExpectedOutput, $"{fixture.Id} output", failures);
            }

            // Act
            if (record.Status == "accepted" && record.ExpectedOutput is null)
            {
                failures.Add($"{fixture.Id}: accepted baseline has no expected output.");
            // Assert
            }
        }

        failures.Count.Should().Be(0, string.Join(Environment.NewLine, failures));
    }

    [TestMethod]
    public void LoadManifest_GeometryDashboardExport_ContainsAcceptedLockedObjects()
    {
        // Arrange
        var path = Path.Combine(RepositoryRoot, "tests", "fixtures", "wave0", "geometry-dashboard",
            "expected", "locked-virtual-objects.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));

        // Act
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        var exportedObjects = new List<JsonElement>();
        foreach (var property in document.RootElement.EnumerateObject().Where(property => property.Name != "$type"))
        {
            var typeName = property.Name.Split(',')[0].Split('.').Last();
            var items = property.Value.EnumerateArray().ToArray();
            counts[typeName] = items.Length;
            exportedObjects.AddRange(items);
        // Assert
        }

        counts["RelevantHitObject"].Should().Be(0);
        counts["RelevantPoint"].Should().Be(10);
        counts["RelevantCircle"].Should().Be(2);
        exportedObjects.Count.Should().Be(12);
        exportedObjects.All(item => item.GetProperty("IsLocked").GetBoolean()).Should().BeTrue();
        exportedObjects.All(item => !item.GetProperty("IsSelected").GetBoolean()).Should().BeTrue();
        exportedObjects.All(item => item.GetProperty("IsInheritable").GetBoolean()).Should().BeTrue();
        exportedObjects.All(item => !item.GetProperty("Disposed").GetBoolean()).Should().BeTrue();
        exportedObjects.All(item => item.GetProperty("DoNotDispose").GetBoolean()).Should().BeTrue();
    }

    private static FixtureManifest LoadManifest()
    {
        var json = File.ReadAllText(ManifestPath);
        return JsonSerializer.Deserialize<FixtureManifest>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidDataException("Fixture manifest is empty.");
    }

    private static void VerifyRelativeFile(string recordPath, string relativePath, string label, ICollection<string> failures)
    {
        var path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(recordPath)!, relativePath));
        if (!File.Exists(path))
        {
            failures.Add($"{label}: missing {Path.GetRelativePath(RepositoryRoot, path)}");
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Mapping_Tools.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not find Mapping_Tools.sln.");
    }

    private sealed record FixtureManifest(Fixture[] Fixtures, DestructiveFeature[] DestructiveFeatures);
    private sealed record Fixture(string Id, string Group, string Path, string? Sha256);
    private sealed record DestructiveFeature(string Id, string BaselineFixtureId);
    private sealed record BaselineRecord(string Status, string SeedInput, string? Options, string? ExpectedOutput);
}
