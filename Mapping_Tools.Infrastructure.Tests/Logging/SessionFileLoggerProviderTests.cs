using Mapping_Tools.Infrastructure.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Infrastructure.Tests.Logging;

[TestClass]
public sealed class SessionFileLoggerProviderTests
{
    [TestMethod]
    public void Constructor_WithOldAndUnrelatedFiles_RemovesOnlyOldApplicationLogs()
    {
        // Arrange
        string directory = CreateDirectory();
        string oldLog = Path.Combine(directory, "mapping-tools-20200101-000.log");
        string oldSessionLog = Path.Combine(directory, $"{DateTimeOffset.UtcNow.AddDays(-8).ToUnixTimeSeconds()}.runtime.log");
        string unrelatedFile = Path.Combine(directory, "other.log");
        File.WriteAllText(oldLog, "old");
        File.SetLastWriteTimeUtc(oldLog, DateTime.UtcNow.AddDays(-8));
        File.WriteAllText(oldSessionLog, "old session");
        File.SetLastWriteTimeUtc(oldSessionLog, DateTime.UtcNow.AddDays(-8));
        File.WriteAllText(unrelatedFile, "keep");

        try
        {
            // Act
            using var provider = new SessionFileLoggerProvider(directory);

            // Assert
            File.Exists(oldLog).Should().BeFalse();
            File.Exists(oldSessionLog).Should().BeFalse();
            File.Exists(unrelatedFile).Should().BeTrue();
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [TestMethod]
    public void Log_WhenSessionFileExceedsFiveMegabytes_KeepsSingleSessionFile()
    {
        // Arrange
        string directory = CreateDirectory();

        try
        {
            // Act
            using (var provider = new SessionFileLoggerProvider(directory))
            {
                provider.CreateLogger("Test").LogInformation("{Text}", new string('x', 5 * 1024 * 1024));
                provider.CreateLogger("Test").LogInformation("later entry");
            }

            // Assert
            string[] files = Directory.GetFiles(directory, "*.runtime.log");
            files.Length.Should().Be(1);
            new FileInfo(files[0]).Length.Should().BeGreaterThan(5 * 1024 * 1024);
            File.ReadAllText(files[0]).Should().Contain("later entry");
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [TestMethod]
    public void Constructor_WithMoreThanTwentyRecentLogs_KeepsAllRecentLogs()
    {
        // Arrange
        string directory = CreateDirectory();
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        for (int index = 0; index < 25; index++)
        {
            string path = Path.Combine(directory, $"{timestamp - index}.runtime.log");
            File.WriteAllText(path, "entry");
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddMinutes(-index));
        }

        try
        {
            // Act
            using var provider = new SessionFileLoggerProvider(directory);

            // Assert
            string[] files = Directory.GetFiles(directory, "*.runtime.log");
            files.Length.Should().Be(25);
            files.Any(path => Path.GetFileName(path).StartsWith($"{timestamp - 24}.")).Should().BeTrue();
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [TestMethod]
    public void Log_WithException_WritesCategoryAndExceptionDetails()
    {
        // Arrange
        string directory = CreateDirectory();
        InvalidOperationException failure = new("diagnostic failure");

        try
        {
            // Act
            using (var provider = new SessionFileLoggerProvider(directory))
                provider.CreateLogger("MappingTools.Test").LogError(failure, "Operation {Operation} failed", "sample");

            // Assert
            string log = File.ReadAllText(Directory.GetFiles(directory, "*.runtime.log").Single());
            log.Should().Contain("MappingTools.Test");
            log.Should().NotContain("(0)");
            log.Should().Contain("Operation sample failed");
            log.Should().Contain("InvalidOperationException: diagnostic failure");
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [TestMethod]
    public void Log_AcrossProviderInstances_CreatesSeparateSessionFiles()
    {
        // Arrange
        string directory = CreateDirectory();

        try
        {
            // Act
            using (var first = new SessionFileLoggerProvider(directory))
                first.CreateLogger("Test").LogInformation("first launch");
            using (var second = new SessionFileLoggerProvider(directory))
                second.CreateLogger("Test").LogInformation("second launch");

            // Assert
            string[] files = Directory.GetFiles(directory, "*.runtime.log");
            files.Length.Should().Be(2);
            files.Count(path => File.ReadAllText(path).Contains("first launch")).Should().Be(1);
            files.Count(path => File.ReadAllText(path).Contains("second launch")).Should().Be(1);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static string CreateDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), "MappingToolsLogsTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}
