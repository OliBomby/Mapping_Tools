using System.Text;
using Mapping_Tools.Infrastructure.Files;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Infrastructure.Tests.Files;

[TestClass]
public sealed class PhysicalAtomicFileWriterTests : IDisposable
{
    private readonly string root = Path.Combine(
        Path.GetTempPath(),
        "mapping-tools-atomic-writer-" + Guid.NewGuid().ToString("N"));

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

    public void Dispose()
    {
        if (Directory.Exists(root)) Directory.Delete(root, true);
    }

    [TestMethod]
    public void WriteText_ReplacesExistingDestinationAndLeavesNoTemporaryFiles()
    {
        // Arrange
        string destination = Path.Combine(root, "document.json");
        File.WriteAllText(destination, "previous");

        // Act
        PhysicalAtomicFileWriter.WriteText(
            destination,
            "complete",
            PhysicalAtomicFileWriter.Utf8WithoutBom);

        // Assert
        File.ReadAllText(destination, Encoding.UTF8).Should().Be("complete");
        Directory.EnumerateFiles(root)
            .Select(Path.GetFileName)
            .Should().Equal("document.json");
    }

    [TestMethod]
    public async Task WriteTextAsync_WhenCancelledBeforeCommit_PreservesExistingDestination()
    {
        // Arrange
        string destination = Path.Combine(root, "document.json");
        File.WriteAllText(destination, "previous");
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        // Act
        Func<Task> act = () => PhysicalAtomicFileWriter.WriteTextAsync(
            destination,
            "replacement",
            PhysicalAtomicFileWriter.Utf8WithoutBom,
            cancellation.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        File.ReadAllText(destination).Should().Be("previous");
        Directory.EnumerateFiles(root)
            .Select(Path.GetFileName)
            .Should().Equal("document.json");
    }
}
