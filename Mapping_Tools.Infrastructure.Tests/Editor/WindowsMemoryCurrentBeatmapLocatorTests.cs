using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Mapping_Tools.Infrastructure.Editor;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Infrastructure.Tests.Editor;

[TestClass]
[SuppressMessage("ReSharper", "AccessToDisposedClosure")]
public sealed class WindowsMemoryCurrentBeatmapLocatorTests
{
    [TestMethod]
    public async Task FindCurrentBeatmapAsync_WhenOsuIsOutsideEditor_UsesInGameMemoryReader()
    {
        // Arrange
        const string expected_path = @"C:\osu!\Songs\123 Artist - Title\map.osu";
        int memoryReadCount = 0;
        WindowsMemoryCurrentBeatmapLocator sut = new(() => true,
            Process.GetCurrentProcess,
            _ =>
            {
                memoryReadCount++;
                return expected_path;
            });

        // Act
        string result = await sut.FindCurrentBeatmapAsync();

        // Assert
        result.Should().Be(expected_path);
        memoryReadCount.Should().Be(1);
    }

    [TestMethod]
    public async Task FindCurrentBeatmapAsync_WhenOsuIsClosed_ThrowsUnavailableError()
    {
        // Arrange
        WindowsMemoryCurrentBeatmapLocator sut = new(() => true,
            () => null,
            _ => "unused.osu");

        // Act
        Func<Task> act = () => sut.FindCurrentBeatmapAsync();

        // Assert
        var exception = await act.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Message.Should().Contain("Open a beatmap in osu!");
    }
}
