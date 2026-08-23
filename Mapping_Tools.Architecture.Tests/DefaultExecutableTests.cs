using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Architecture.Tests;

[TestClass]
public sealed class DefaultExecutableTests
{
    private static readonly string repositoryRoot = FindRepositoryRoot();

    [TestMethod]
    public void Solution_ContainsOnlyAvaloniaFrontend()
    {
        // Arrange
        string solution = File.ReadAllText(Path.Combine(repositoryRoot, "Mapping_Tools.slnx"));
        string[] projectPaths = XDocument.Parse(solution)
            .Descendants("Project")
            .Select(element => element.Attribute("Path")?.Value)
            .Where(path => path is not null)
            .Select(path => path!)
            .ToArray();

        // Act
        bool containsAvaloniaFrontend = projectPaths.Contains(
            "Mapping_Tools.Desktop/Mapping_Tools.Desktop.csproj",
            StringComparer.Ordinal);
        bool containsLegacyFrontend = projectPaths.Contains(
            "Mapping_Tools/Mapping_Tools.csproj",
            StringComparer.Ordinal);

        // Assert
        containsAvaloniaFrontend.Should().BeTrue();
        containsLegacyFrontend.Should().BeFalse();
        solution.Should().NotContain("Mapping_Tools_Tests");
    }

    [TestMethod]
    public void ReleaseAndDevelopmentEntryPoints_TargetAvaloniaOnly()
    {
        // Arrange
        string workflow = File.ReadAllText(Path.Combine(repositoryRoot, ".github", "workflows", "release.yml"));
        string launch = File.ReadAllText(Path.Combine(repositoryRoot, ".vscode", "launch.json"));
        string tasks = File.ReadAllText(Path.Combine(repositoryRoot, ".vscode", "tasks.json"));
        string installer = File.ReadAllText(Path.Combine(repositoryRoot, "Installer_Script_x64.iss"));
        string updater = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "Mapping_Tools.Infrastructure",
            "Updates",
            "OnovaUpdateGateway.cs"));

        // Act
        bool primaryWorkflowIsAvalonia = workflow.Contains(
            "dotnet publish Mapping_Tools.Desktop/Mapping_Tools.Desktop.csproj",
            StringComparison.Ordinal);
        bool legacyWorkflowIsRemoved = workflow.Contains(
                                           "legacy WPF fallback",
                                           StringComparison.Ordinal)
                                       || workflow.Contains(
                                           "dotnet publish Mapping_Tools/Mapping_Tools.csproj",
                                           StringComparison.Ordinal);

        // Assert
        primaryWorkflowIsAvalonia.Should().BeTrue();
        legacyWorkflowIsRemoved.Should().BeFalse();
        workflow.Should().NotContain("legacy-wpf");
        workflow.Should().NotContain("Mapping_Tools/Mapping_Tools.csproj");
        launch.Should().Contain("Mapping_Tools.Desktop/bin/Debug/net10.0/Mapping_Tools.Desktop.dll");
        tasks.Should().Contain(
            "\"${workspaceFolder}/Mapping_Tools.Desktop/Mapping_Tools.Desktop.csproj\"");
        tasks.Should().Contain("\"--runtime\"");
        tasks.Should().Contain("\"win-x64\"");
        installer.Should().Contain("Mapping_Tools.Desktop\\bin\\Release\\net10.0\\win-x64\\publish");
        installer.Should().Contain("#define MyAppExeName \"Mapping Tools.exe\"");
        updater.Should().Contain("AssemblyMetadata.FromAssembly(entryAssembly, publishedExecutablePath)");
        updater.Should().Contain("PublishedExecutableName = \"Mapping Tools.exe\"");
        workflow.Should().Contain("-ExpectedVersion \"${env:VERSION}\"");
        workflow.Should().Contain("mapping_tools_installer_x86.exe");
        workflow.Should().Contain("mapping_tools_installer_x64.exe");
    }

    [TestMethod]
    public void ReleaseValidation_RequiresPrimaryArchivesAndInstallerOutputs()
    {
        // Arrange
        string validator = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "tools",
            "validate-release-layout.ps1"));

        // Assert
        validator.Should().Contain("Assert-ArchiveLayout $PrimaryArchiveX86");
        validator.Should().Contain("Assert-ArchiveLayout $PrimaryArchiveX64");
        validator.Should().NotContain("$LegacyArchive");
        validator.Should().Contain("Mapping_Tools.Desktop.dll");
        validator.Should().Contain("Mapping Tools.dll");
        validator.Should().Contain("$InstallerX86");
        validator.Should().Contain("$InstallerX64");
    }

    [TestMethod]
    public void LegacyFrontend_AndLegacyTestProject_AreRemoved()
    {
        // Arrange
        string legacyFrontendDirectory = Path.Combine(repositoryRoot, "Mapping_Tools");
        string legacyTestDirectory = Path.Combine(repositoryRoot, "Mapping_Tools_Tests");

        // Act
        bool legacyFrontendExists = Directory.Exists(legacyFrontendDirectory);
        bool legacyTestProjectExists = Directory.Exists(legacyTestDirectory);

        // Assert
        legacyFrontendExists.Should().BeFalse();
        legacyTestProjectExists.Should().BeFalse();
        File.Exists(Path.Combine(repositoryRoot, "Mapping_Tools.Desktop", "Mapping_Tools.Desktop.csproj"))
            .Should().BeTrue();
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Mapping_Tools.slnx")))
            directory = directory.Parent;

        return directory?.FullName
               ?? throw new DirectoryNotFoundException("Could not find Mapping_Tools.slnx.");
    }
}
