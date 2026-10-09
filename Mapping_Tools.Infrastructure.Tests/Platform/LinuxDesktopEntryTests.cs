using Mapping_Tools.Infrastructure.Platform;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Infrastructure.Tests.Platform;

[TestClass]
public sealed class LinuxDesktopEntryTests
{
    private string root = null!;

    [TestInitialize]
    public void Initialize()
    {
        root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(root)) Directory.Delete(root, true);
    }

    [TestMethod]
    public void EnsureInstalled_WithDirectAppImage_CreatesIdentifiableUserLauncher()
    {
        // Arrange
        string executable = "/home/mapper/Mapping Tools.AppImage";

        // Act
        LinuxDesktopEntry.EnsureInstalled([root], executable, null);

        // Assert
        string contents = File.ReadAllText(Path.Combine(root, "applications", "MappingTools.desktop"));
        contents.Should().Contain("Name=Mapping Tools");
        contents.Should().Contain("Icon=MappingTools");
        contents.Should().Contain("Exec=\"/home/mapper/Mapping Tools.AppImage\"");
    }

    [TestMethod]
    public void EnsureInstalled_WithBundledIcon_InstallsIconInUserThemeDirectory()
    {
        // Arrange
        Directory.CreateDirectory(root);
        string source = Path.Combine(root, "bundled-icon.png");
        byte[] image = [137, 80, 78, 71];
        File.WriteAllBytes(source, image);

        // Act
        LinuxDesktopEntry.EnsureInstalled([root], "/app/MappingTools.AppImage", null, source);

        // Assert
        File.ReadAllBytes(Path.Combine(root, "icons", "hicolor", "256x256", "apps", "MappingTools.png"))
            .Should().Equal(image);
        File.ReadAllText(Path.Combine(root, "applications", "MappingTools.desktop"))
            .Should().Contain("Icon=MappingTools");
    }

    [TestMethod]
    public void EnsureInstalled_WithChangedIconAndUnchangedLauncher_UpdatesInstalledIcon()
    {
        // Arrange
        Directory.CreateDirectory(root);
        string source = Path.Combine(root, "bundled-icon.png");
        File.WriteAllBytes(source, [1, 2, 3]);
        LinuxDesktopEntry.EnsureInstalled([root], "/app/MappingTools.AppImage", null, source);
        byte[] updated = [4, 5, 6];
        File.WriteAllBytes(source, updated);

        // Act
        LinuxDesktopEntry.EnsureInstalled([root], "/app/MappingTools.AppImage", null, source);

        // Assert
        File.ReadAllBytes(Path.Combine(root, "icons", "hicolor", "256x256", "apps", "MappingTools.png"))
            .Should().Equal(updated);
    }

    [TestMethod]
    public void EnsureInstalled_WithMovedAppImage_UpdatesGeneratedLauncher()
    {
        // Arrange
        LinuxDesktopEntry.EnsureInstalled([root], "/old/MappingTools.AppImage", null);

        // Act
        LinuxDesktopEntry.EnsureInstalled([root], "/new/MappingTools.AppImage", null);

        // Assert
        string contents = File.ReadAllText(Path.Combine(root, "applications", "MappingTools.desktop"));
        contents.Should().Contain("Exec=\"/new/MappingTools.AppImage\"");
        contents.Should().NotContain("/old/");
    }

    [TestMethod]
    public void EnsureInstalled_WithUserLauncher_PreservesCustomIntegration()
    {
        // Arrange
        string directory = Path.Combine(root, "applications");
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "MappingTools.desktop");
        const string custom = "[Desktop Entry]\nName=My Mapping Tools\nExec=my-wrapper\n";
        File.WriteAllText(path, custom);

        // Act
        LinuxDesktopEntry.EnsureInstalled([root], "/app/MappingTools.AppImage", null);

        // Assert
        File.ReadAllText(path).Should().Be(custom);
    }

    [TestMethod]
    public void EnsureInstalled_WithSystemLauncher_DoesNotShadowPackageIntegration()
    {
        // Arrange
        string system = Path.Combine(root, "system");
        string user = Path.Combine(root, "user");
        Directory.CreateDirectory(Path.Combine(system, "applications"));
        File.WriteAllText(Path.Combine(system, "applications", "MappingTools.desktop"), "[Desktop Entry]");

        // Act
        LinuxDesktopEntry.EnsureInstalled([user, system], "/app/MappingTools.AppImage", null);

        // Assert
        Directory.Exists(user).Should().BeFalse();
    }

    [TestMethod]
    public void CreateContents_WithDotnetHostAndReservedCharacters_QuotesBothArgumentsAndEscapesFieldCodes()
    {
        // Arrange
        const string executable = "/usr/bin/dotnet";
        const string assembly = "/home/mapper/$maps/100%/Mapping Tools.Desktop.dll";

        // Act
        string contents = LinuxDesktopEntry.CreateContents(executable, assembly);

        // Assert
        contents.Should().Contain("Exec=\"/usr/bin/dotnet\" \"/home/mapper/\\\\$maps/100%%/Mapping Tools.Desktop.dll\"");
    }
}
