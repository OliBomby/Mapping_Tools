using Mapping_Tools.Infrastructure.MapsetMerger;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Infrastructure.Tests.MapsetMerger;

[TestClass]
public sealed class PhysicalMapsetFileSystemTests : IDisposable
{
    private readonly string root = Path.Combine(
        Path.GetTempPath(),
        "mapping-tools-mapset-transaction-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(root)) Directory.Delete(root, true);
    }

    [TestInitialize]
    public void Initialize()
    {
        Directory.CreateDirectory(root);
    }

    [TestCleanup]
    public void Cleanup()
    {
        Dispose();
    }

    [TestMethod]
    public async Task CommitAsync_WhenReplacementFails_RestoresOriginalDuplicateTarget()
    {
        // Arrange
        string export = Path.Combine(root, "export");
        Directory.CreateDirectory(export);
        string original = Path.Combine(export, "same.txt");
        File.WriteAllText(original, "original");
        string firstSource = Path.Combine(root, "first.txt");
        string secondSource = Path.Combine(root, "second.txt");
        File.WriteAllText(firstSource, "first");
        File.WriteAllText(secondSource, "second");

        using var transaction =
            new PhysicalMapsetFileSystem().BeginTransaction(export);
        transaction.CopyToStaging(firstSource, "same.txt");
        transaction.CopyToStaging(secondSource, "same.txt");
        transaction.CopyToStaging(firstSource, "new-directory/new.txt");
        Directory.CreateDirectory(Path.Combine(export, "failure.txt"));
        transaction.CopyToStaging(firstSource, "failure.txt");

        // Act
        var act = () => transaction.CommitAsync();

        // Assert
        await act.Should().ThrowAsync<IOException>();
        File.ReadAllText(original).Should().Be("original");
        Directory.Exists(Path.Combine(export, "new-directory")).Should().BeFalse();
        Directory.Exists(Path.Combine(export, "failure.txt")).Should().BeTrue();
        Directory.GetDirectories(root, ".export.mapset-merger-*", SearchOption.TopDirectoryOnly)
            .Should().BeEmpty();
    }
}
