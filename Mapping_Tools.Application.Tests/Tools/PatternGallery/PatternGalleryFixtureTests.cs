using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tests.Execution;
using Mapping_Tools.Application.Tools.PatternGallery;
using Mapping_Tools.Application.Tools.PatternGallery.Models;
using Mapping_Tools.Infrastructure.Tools.PatternGallery;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.Tools.PatternGallery;

[TestClass]
public sealed class PatternGalleryFixtureTests : TransformationFixtureTestBase
{
    [DataTestMethod]
    [DataRow("000")]
    public async Task ExportAsync_AcceptedFixture_ProducesEquivalentOutput(string fixtureName)
    {
        // Arrange
        using var fixture = CreateFixture("pattern-gallery", fixtureName);
        var project = fixture.ReadProject<PatternGalleryServiceOptions>();
        var paths = ReadPatternGalleryPaths(fixture);
        var pattern = project.Patterns.Single(item =>
            item.Name.Equals(fixture.Options.GetProperty("Pattern").GetString(), StringComparison.Ordinal));
        PatternGalleryService service = new(
            fixture.Gateway,
            new PatternGalleryFileService(),
            new ApplicationSettings());

        // Act
        await service.ExportAsync(
            fixture.TargetPath,
            [pattern],
            project,
            paths,
            false,
            cancellationToken: CancellationToken.None);
        FixtureExecutionResult actual = new([fixture.TargetPath]);

        // Assert
        fixture.AssertAccepted("Pattern Gallery");
        fixture.AssertTextOutput(actual);
    }

    private static PatternGalleryCollectionPaths ReadPatternGalleryPaths(FixtureContext fixture)
    {
        string projectPath = fixture.RequiredPath("PatternCollection");
        string patternFile = fixture.RequiredPath("PatternFile");
        string collectionRoot = Path.GetDirectoryName(projectPath)!;
        return new PatternGalleryCollectionPaths(
            Path.GetDirectoryName(collectionRoot)!,
            collectionRoot,
            Path.GetDirectoryName(patternFile)!,
            projectPath);
    }
}
