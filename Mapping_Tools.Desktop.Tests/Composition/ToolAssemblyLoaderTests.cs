using Mapping_Tools.Desktop.Composition;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Composition;

[TestClass]
public sealed class ToolAssemblyLoaderTests
{
    [TestMethod]
    public void Load_WithMissingPluginDirectory_CreatesDirectoryAndReturnsBuiltInAssembly()
    {
        // Arrange
        string root = Path.Combine(
            Path.GetTempPath(),
            "Mapping Tools Tests",
            Guid.NewGuid().ToString());
        string pluginDirectory = Path.Combine(root, "Plugins");

        // Act
        try
        {
            var assemblies = ToolAssemblyLoader.Load(pluginDirectory);

            // Assert
            Directory.Exists(pluginDirectory).Should().BeTrue();
            assemblies.Should().ContainSingle(assembly =>
                assembly == typeof(ToolAssemblyLoader).Assembly);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [TestMethod]
    public void Load_WithInvalidPluginAssembly_ReturnsOtherAssemblies()
    {
        // Arrange
        string root = Path.Combine(
            Path.GetTempPath(),
            "Mapping Tools Tests",
            Guid.NewGuid().ToString());
        string pluginDirectory = Path.Combine(root, "Plugins");
        Directory.CreateDirectory(pluginDirectory);
        File.WriteAllText(Path.Combine(pluginDirectory, "invalid-plugin.dll"), "not an assembly");

        // Act
        try
        {
            var assemblies = ToolAssemblyLoader.Load(pluginDirectory);

            // Assert
            assemblies.Should().ContainSingle(assembly =>
                assembly == typeof(ToolAssemblyLoader).Assembly);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
