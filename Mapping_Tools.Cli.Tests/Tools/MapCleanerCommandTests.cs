using AwesomeAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Cli.Tests.Tools;

[TestClass]
public sealed class MapCleanerCommandTests
{
    [TestMethod]
    public async Task RunAsync_MultipleBeatmaps_ReportsEachSavedMapAndOneBatchSummary()
    {
        // Arrange
        string directory = Path.Combine(Path.GetTempPath(), "mapping-tools-cleaner-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string first = CopyBeatmap(directory, "first.osu");
        string second = CopyBeatmap(directory, "second.osu");

        try
        {
            // Act
            var result = await CliRunnerTests.RunAsync(
                "map-cleaner", first, second, "--editor-reader", "disabled", "--no-backups", "--analyze-samples", "false");

            // Assert
            result.Code.Should().Be(0, result.Error);
            result.Output.Should().Contain("Processed " + first).And.Contain("Processed " + second);
            result.Output.Split(Environment.NewLine).Count(line => line.StartsWith("Completed:", StringComparison.Ordinal)).Should().Be(1);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [TestMethod]
    public async Task RunAsync_LaterBeatmapIsMissing_RejectsBatchBeforeEditingAnyTargets()
    {
        // Arrange
        string directory = Path.Combine(Path.GetTempPath(), "mapping-tools-cleaner-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string first = CopyBeatmap(directory, "first.osu");
        string missing = Path.Combine(directory, "missing.osu");
        byte[] original = await File.ReadAllBytesAsync(first);

        try
        {
            // Act
            var result = await CliRunnerTests.RunAsync(
                "map-cleaner", first, missing, "--editor-reader", "disabled", "--no-backups", "--analyze-samples", "false");

            // Assert
            result.Code.Should().Be(1);
            result.Error.Should().Contain("missing.osu");
            result.Output.Should().NotContain("Processed ");
            (await File.ReadAllBytesAsync(first)).Should().Equal(original);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static string CopyBeatmap(string directory, string name)
    {
        string destination = Path.Combine(directory, name);
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Fixtures", "standard-autofail-2b.osu"), destination);

        return destination;
    }
}
