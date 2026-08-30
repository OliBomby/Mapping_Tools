using System.Diagnostics;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Infrastructure.Files;
using Mapping_Tools.Infrastructure.Editor;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Infrastructure.Tests.Editor;

[TestClass]
public sealed class WindowsEditorReaderAdapterTests
{
    [TestMethod]
    public async Task FindCurrentBeatmapAsync_WhenOsuIsOutsideEditor_UsesInGameMemoryReader()
    {
        // Arrange
        const string expectedPath = @"C:\osu!\Songs\123 Artist - Title\map.osu";
        int memoryReadCount = 0;
        WindowsEditorReaderAdapter sut = new(
            new ApplicationSettings
            {
                SongsPath = @"C:\osu!\Songs",
                UseEditorReader = false,
            },
            new ApplicationDirectories(Path.Combine(
                Path.GetTempPath(),
                "Mapping Tools Tests")),
            () => true,
            Process.GetCurrentProcess,
            _ =>
            {
                memoryReadCount++;
                return expectedPath;
            });

        // Act
        string? result = await sut.FindCurrentBeatmapAsync();

        // Assert
        result.Should().Be(expectedPath);
        memoryReadCount.Should().Be(1);
        sut.Dispose();
    }
}
