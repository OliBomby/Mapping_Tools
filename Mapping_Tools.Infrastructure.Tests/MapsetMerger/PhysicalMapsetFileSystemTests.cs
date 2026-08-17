using FluentAssertions;
using Mapping_Tools.Application.MapsetMerger;
using Mapping_Tools.Infrastructure.MapsetMerger;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Infrastructure.Tests.MapsetMerger;

[TestClass]
public sealed class PhysicalMapsetFileSystemTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "mapping-tools-mapset-transaction-" + Guid.NewGuid().ToString("N"));

    [TestInitialize]
    public void Initialize() => Directory.CreateDirectory(_root);

    [TestCleanup]
    public void Cleanup() => Dispose();

    [TestMethod]
    public async Task CommitAsync_WhenReplacementFails_RestoresOriginalDuplicateTarget()
    {
        // Arrange
        string export = Path.Combine(_root, "export");
        Directory.CreateDirectory(export);
        string original = Path.Combine(export, "same.txt");
        File.WriteAllText(original, "original");
        string firstSource = Path.Combine(_root, "first.txt");
        string secondSource = Path.Combine(_root, "second.txt");
        File.WriteAllText(firstSource, "first");
        File.WriteAllText(secondSource, "second");

        using IMapsetFileTransaction transaction =
            new PhysicalMapsetFileSystem().BeginTransaction(export);
        transaction.CopyToStaging(firstSource, "same.txt");
        transaction.CopyToStaging(secondSource, "same.txt");
        transaction.CopyToStaging(firstSource, "new-directory/new.txt");
        Directory.CreateDirectory(Path.Combine(export, "failure.txt"));
        transaction.CopyToStaging(firstSource, "failure.txt");

        // Act
        Func<Task> act = () => transaction.CommitAsync();

        // Assert
        await act.Should().ThrowAsync<IOException>();
        File.ReadAllText(original).Should().Be("original");
        Directory.Exists(Path.Combine(export, "new-directory")).Should().BeFalse();
        Directory.Exists(Path.Combine(export, "failure.txt")).Should().BeTrue();
        Directory.GetDirectories(_root, ".export.mapset-merger-*", SearchOption.TopDirectoryOnly)
            .Should().BeEmpty();
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
