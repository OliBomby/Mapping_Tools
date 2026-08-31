using System.ComponentModel;
using System.Diagnostics;
using Mapping_Tools.Infrastructure.Platform;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Infrastructure.Tests.Platform;

[TestClass]
public sealed class PortableFileRevealServiceTests
{
    [TestMethod]
    public async Task RevealAsync_OnWindowsFile_UsesExplorerSelectionArguments()
    {
        // Arrange
        using TestPath testPath = TestPath.File();
        ProcessStartInfo? started = null;
        PortableFileRevealService service = new(
            () => PortableFileRevealPlatform.Windows,
            startInfo =>
            {
                started = startInfo;
                return null;
            });

        // Act
        await service.RevealAsync(testPath.Path);

        // Assert
        started.Should().NotBeNull();
        started!.FileName.Should().Be("explorer.exe");
        started.ArgumentList.Should().Equal("/select,", Path.GetFullPath(testPath.Path));
    }

    [TestMethod]
    public async Task RevealAsync_OnMacOsFile_UsesOpenRevealArguments()
    {
        // Arrange
        using TestPath testPath = TestPath.File();
        ProcessStartInfo? started = null;
        PortableFileRevealService service = new(
            () => PortableFileRevealPlatform.MacOs,
            startInfo =>
            {
                started = startInfo;
                return null;
            });

        // Act
        bool result = await service.RevealAsync(testPath.Path);

        // Assert
        result.Should().BeFalse();
        started.Should().NotBeNull();
        started!.FileName.Should().Be("open");
        started.ArgumentList.Should().Equal("-R", Path.GetFullPath(testPath.Path));
    }

    [TestMethod]
    public async Task RevealAsync_OnLinuxFile_OpensContainingDirectory()
    {
        // Arrange
        using TestPath testPath = TestPath.File();
        ProcessStartInfo? started = null;
        PortableFileRevealService service = new(
            () => PortableFileRevealPlatform.Linux,
            startInfo =>
            {
                started = startInfo;
                return null;
            });

        // Act
        await service.RevealAsync(testPath.Path);

        // Assert
        started.Should().NotBeNull();
        started!.FileName.Should().Be("xdg-open");
        started.ArgumentList.Should().Equal(Path.GetDirectoryName(Path.GetFullPath(testPath.Path))!);
    }

    [TestMethod]
    public async Task RevealAsync_WhenFileManagerExecutableIsMissing_ReturnsFalse()
    {
        // Arrange
        using TestPath testPath = TestPath.Directory();
        PortableFileRevealService service = new(
            () => PortableFileRevealPlatform.Linux,
            _ => throw new Win32Exception("xdg-open is unavailable"));

        // Act
        bool result = await service.RevealAsync(testPath.Path);

        // Assert
        result.Should().BeFalse();
    }

    private sealed class TestPath : IDisposable
    {
        private TestPath(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public static TestPath File()
        {
            string directory = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "MappingToolsFileRevealTests",
                Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(directory);
            string path = System.IO.Path.Combine(directory, "map.osu");
            System.IO.File.WriteAllText(path, string.Empty);
            return new TestPath(path);
        }

        public static TestPath Directory()
        {
            string path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "MappingToolsFileRevealTests",
                Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(path);
            return new TestPath(path);
        }

        public void Dispose()
        {
            string? directory = System.IO.Directory.Exists(Path)
                ? Path
                : System.IO.Path.GetDirectoryName(Path);
            if (directory is not null && System.IO.Directory.Exists(directory))
                System.IO.Directory.Delete(directory, true);
        }
    }
}
