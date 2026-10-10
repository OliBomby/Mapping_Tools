using Mapping_Tools.Desktop.Composition;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Composition;

[TestClass]
public sealed class DesktopStartupArgumentsTests
{
    [TestMethod]
    public void GetLocalUpdatePackagePath_WithSeparatePath_ReturnsFullPath()
    {
        // Arrange
        string relativePath = Path.Combine(".", $"local-update-{Guid.NewGuid():N}.zip");
        string fullPath = Path.GetFullPath(relativePath);
        File.WriteAllText(fullPath, string.Empty);

        try
        {
            // Act
            string? result = DesktopStartupArguments.GetLocalUpdatePackagePath(
                ["--update-file", relativePath]);

            // Assert
            result.Should().Be(fullPath);
        }
        finally
        {
            File.Delete(fullPath);
        }
    }

    [TestMethod]
    public void GetLocalUpdatePackagePath_WithEqualsSyntax_ReturnsFullPath()
    {
        // Arrange
        string path = Path.GetTempFileName();

        try
        {
            // Act
            string? result = DesktopStartupArguments.GetLocalUpdatePackagePath(
                [$"--update-file={path}"]);

            // Assert
            result.Should().Be(Path.GetFullPath(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void GetLocalUpdatePackagePath_WithoutOption_ReturnsNull()
    {
        // Arrange
        string[] arguments = ["--some-other-option"];

        // Act
        string? result = DesktopStartupArguments.GetLocalUpdatePackagePath(arguments);

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public void GetLocalUpdatePackagePath_WithMissingPath_ThrowsArgumentException()
    {
        // Arrange
        string[] arguments = ["--update-file"];

        // Act
        Action act = () => DesktopStartupArguments.GetLocalUpdatePackagePath(arguments);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
