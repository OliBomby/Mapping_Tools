using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Mapping_Tools.Infrastructure.Files;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Infrastructure.Tests.Files;

[TestClass]
[SuppressMessage("ReSharper", "AccessToDisposedClosure")]
public sealed class PhysicalFileWriterTests : IDisposable
{
    private readonly string root = Path.Combine(
        Path.GetTempPath(),
        "mapping-tools-file-writer-" + Guid.NewGuid().ToString("N"));

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
    public void WriteText_ReplacesExistingDestinationAndLeavesNoTemporaryFiles()
    {
        // Arrange
        string destination = Path.Combine(root, "document.json");
        File.WriteAllText(destination, "previous");

        // Act
        PhysicalFileWriter.WriteText(
            destination,
            "complete",
            PhysicalFileWriter.Utf8WithoutBom);

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
        await File.WriteAllTextAsync(destination, "previous");
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();

        // Act
        var act = () => PhysicalFileWriter.WriteTextAsync(
            destination,
            "replacement",
            PhysicalFileWriter.Utf8WithoutBom,
            cancellation.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        File.ReadAllText(destination).Should().Be("previous");
        Directory.EnumerateFiles(root)
            .Select(Path.GetFileName)
            .Should().Equal("document.json");
    }

    [TestMethod]
    [DataRow("text", 20)]
    [DataRow("text", 225)]
    [DataRow("text-async", 20)]
    [DataRow("text-async", 225)]
    [DataRow("lines", 20)]
    [DataRow("lines", 225)]
    [DataRow("copy", 20)]
    [DataRow("copy", 225)]
    public async Task Write_ExistingDestination_DoesNotNotifyTemporaryPaths(
        string operation, int fileNameLength)
    {
        // Arrange
        string fileName = new string('a', fileNameLength - 4) + ".osu";
        string destination = Path.Combine(root, fileName);
        string source = Path.Combine(root, "source.osu");
        await File.WriteAllTextAsync(destination, "previous longer contents");
        await File.WriteAllTextAsync(source, "complete");
        ConcurrentQueue<(WatcherChangeTypes Type, string? Name)> events = new();
        TaskCompletionSource sentinelCreated = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource destinationNotified = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using FileSystemWatcher watcher = new(root);
        watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size;
        watcher.Changed += recordEvent;
        watcher.Deleted += recordEvent;
        watcher.Created += recordEvent;
        watcher.Renamed += (sender, args) =>
        {
            events.Enqueue((args.ChangeType, args.OldName));
            recordEvent(sender, args);
        };
        watcher.Error += (_, args) => sentinelCreated.TrySetException(args.GetException());
        watcher.EnableRaisingEvents = true;

        void recordEvent(object _, FileSystemEventArgs args)
        {
            events.Enqueue((args.ChangeType, args.Name));
            if (args.Name == "sentinel" && args.ChangeType == WatcherChangeTypes.Created)
                sentinelCreated.TrySetResult();
            if (args.Name == fileName) destinationNotified.TrySetResult();
        }

        // Act
        switch (operation)
        {
            case "text":
                PhysicalFileWriter.WriteText(destination, "complete", PhysicalFileWriter.Utf8WithoutBom);
                break;
            case "text-async":
                await PhysicalFileWriter.WriteTextAsync(destination, "complete", PhysicalFileWriter.Utf8WithoutBom);
                break;
            case "lines":
                await PhysicalFileWriter.WriteLinesAsync(destination, ["complete"], PhysicalFileWriter.Utf8WithoutBom, "\r\n");
                break;
            case "copy":
                await PhysicalFileWriter.CopyAsync(source, destination);
                break;
        }

        // Wait for both paths: macOS can coalesce and reorder notifications within a batch.
        await File.WriteAllTextAsync(Path.Combine(root, "sentinel"), "");
        await Task.WhenAll(sentinelCreated.Task, destinationNotified.Task).WaitAsync(TimeSpan.FromSeconds(10));

        // Assert
        File.ReadAllText(destination).Should().Be(operation == "lines" ? "complete\r\n" : "complete");
        var writeEvents = events.Where(item => item.Name != "sentinel").ToArray();
        writeEvents.Should().NotBeEmpty();
        // FSEvents can include delayed setup events and report existing files as Created.
        // Every reported path must still belong to a real input or destination, never a temporary sibling.
        writeEvents.Should().OnlyContain(item => item.Name == fileName || item.Name == "source.osu");
        if (OperatingSystem.IsWindows())
            writeEvents.Should().OnlyContain(item => item.Type == WatcherChangeTypes.Changed && item.Name == fileName);
    }

    [TestMethod]
    public void WriteText_WithByteOrderMark_PreservesEncodingPreamble()
    {
        // Arrange
        string destination = Path.Combine(root, "document.txt");
        Encoding encoding = new UTF8Encoding(true);

        // Act
        PhysicalFileWriter.WriteText(destination, "complete", encoding);

        // Assert
        File.ReadAllBytes(destination).Should().Equal(encoding.GetPreamble().Concat(encoding.GetBytes("complete")));
    }

    [TestMethod]
    public void WriteText_WhenEncodingFails_PreservesExistingDestination()
    {
        // Arrange
        string destination = Path.Combine(root, "document.txt");
        File.WriteAllText(destination, "previous");
        Encoding encoding = new UTF8Encoding(false, true);

        // Act
        Action act = () => PhysicalFileWriter.WriteText(destination, "\uD800", encoding);

        // Assert
        act.Should().Throw<EncoderFallbackException>();
        File.ReadAllText(destination).Should().Be("previous");
    }
}
