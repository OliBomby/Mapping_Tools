using System.Security.Cryptography;
using System.Text.Json;
using Mapping_Tools.Application.Tools.MapsetMerger;
using Mapping_Tools.Application.Tools.MapsetMerger.Models;
using Mapping_Tools.Application.Tests.Execution;
using Mapping_Tools.Infrastructure.Files;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.Tools.MapsetMerger;

[TestClass]
public sealed class MapsetMergerFixtureTests : TransformationFixtureTestBase
{
    [DataTestMethod]
    [DataRow("000")]
    public async Task MergeAsync_AcceptedFixture_ProducesEquivalentOutput(string fixtureName)
    {
        // Arrange
        using FixtureContext fixture = CreateFixture("mapset-merger", fixtureName);
        MapsetMergerServiceOptions project = fixture.ReadProject<MapsetMergerServiceOptions>();
        StageMapsetMergerSources(fixture, project);
        MapsetMergerService service = new(fixture.Gateway, new PhysicalBeatmapsetFileSystem());

        // Act
        await service.MergeAsync(project, cancellationToken: CancellationToken.None);
        FixtureExecutionResult actual = new(OutputDirectory: project.ExportPath);

        // Assert
        fixture.AssertAccepted("Mapset Merger");
        AssertMapsetOutput(fixture, actual);
    }

    private static void StageMapsetMergerSources(FixtureContext fixture, MapsetMergerServiceOptions project)
    {
        string sourceRoot = fixture.ResolveFixturePath("../../../Mapsets/multi-difficulty");
        foreach (var mapsetOptions in fixture.Options.GetProperty("Mapsets").EnumerateArray())
        {
            string name = mapsetOptions.GetProperty("Name").GetString()!;
            var mapset = project.Mapsets.Single(item => item.Name == name);
            Directory.CreateDirectory(mapset.Path);
            string beatmapPath = fixture.ResolveFixturePath(
                mapsetOptions.GetProperty("Beatmaps").EnumerateArray().Single().GetString()!);
            File.Copy(
                beatmapPath,
                Path.Combine(mapset.Path, Path.GetFileName(beatmapPath)),
                true);
            foreach (string asset in fixture.Options.GetProperty("Assets")
                         .EnumerateArray()
                         .Select(item => item.GetString()!))
            {
                string sourceAsset = Path.Combine(sourceRoot, asset);
                if (File.Exists(sourceAsset))
                {
                    File.Copy(sourceAsset, Path.Combine(mapset.Path, asset), true);
                }
            }
        }
    }

    private static void AssertMapsetOutput(FixtureContext fixture, FixtureExecutionResult actual)
    {
        actual.WasExecuted.Should().BeTrue();
        actual.OutputDirectory.Should().NotBeNullOrWhiteSpace();

        using var manifest = JsonDocument.Parse(File.ReadAllText(fixture.ExpectedOutputPath));
        foreach (var item in manifest.RootElement.GetProperty("beatmaps").EnumerateArray())
        {
            string expectedPath = fixture.ResolveFixturePath(item.GetProperty("path").GetString()!);
            string actualPath = Path.Combine(actual.OutputDirectory!, Path.GetFileName(expectedPath));
            AssertTextOutputEquivalent(expectedPath, actualPath);
        }

        foreach (var item in manifest.RootElement.GetProperty("exportedAssets").EnumerateArray())
        {
            string relativePath = item.GetProperty("path").GetString()!;
            string actualPath = Path.Combine(
                actual.OutputDirectory!, relativePath.Replace('/', Path.DirectorySeparatorChar));
            File.Exists(actualPath).Should().BeTrue($"Expected merged asset does not exist: {relativePath}");
            FileHash(actualPath).Should().Be(item.GetProperty("sha256").GetString());
        }
    }

    private static string FileHash(string path)
    {
        using var sha256 = SHA256.Create();
        return Convert.ToHexString(sha256.ComputeHash(File.ReadAllBytes(path)));
    }
}
