using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Architecture.Tests;

[TestClass]
public sealed class DefaultExecutableTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [TestMethod]
    public void Solution_DefaultExecutable_ListsAvaloniaBeforeLegacyWpf()
    {
        // Arrange
        string solution = File.ReadAllText(Path.Combine(RepositoryRoot, "Mapping_Tools.sln"));

        // Act
        int avaloniaIndex = solution.IndexOf(
            "= \"Mapping_Tools.Desktop\",",
            StringComparison.Ordinal);
        int wpfIndex = solution.IndexOf(
            "= \"Mapping_Tools\",",
            StringComparison.Ordinal);

        // Assert
        avaloniaIndex.Should().BeGreaterThanOrEqualTo(0);
        wpfIndex.Should().BeGreaterThanOrEqualTo(0);
        avaloniaIndex.Should().BeLessThan(wpfIndex);
    }

    [TestMethod]
    public void ReleaseAndDevelopmentEntryPoints_TargetAvaloniaAndKeepWpfFallback()
    {
        // Arrange
        string workflow = File.ReadAllText(Path.Combine(RepositoryRoot, ".github", "workflows", "release.yml"));
        string launch = File.ReadAllText(Path.Combine(RepositoryRoot, ".vscode", "launch.json"));
        string tasks = File.ReadAllText(Path.Combine(RepositoryRoot, ".vscode", "tasks.json"));
        string installer = File.ReadAllText(Path.Combine(RepositoryRoot, "Installer_Script_x64.iss"));
        string updater = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "Mapping_Tools.Infrastructure",
            "Updates",
            "OnovaUpdateGateway.cs"));

        // Act
        bool primaryWorkflowIsAvalonia = workflow.Contains(
            "dotnet publish Mapping_Tools.Desktop/Mapping_Tools.Desktop.csproj",
            StringComparison.Ordinal);
        bool fallbackWorkflowIsWpf = workflow.Contains(
            "legacy WPF fallback",
            StringComparison.Ordinal) && workflow.Contains(
            "dotnet publish Mapping_Tools/Mapping_Tools.csproj -c Release",
            StringComparison.Ordinal);

        // Assert
        primaryWorkflowIsAvalonia.Should().BeTrue();
        fallbackWorkflowIsWpf.Should().BeTrue();
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
    public void ReleaseValidation_RequiresBothArchiveFamiliesAndInstallerOutputs()
    {
        // Arrange
        string validator = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "tools",
            "validate-release-layout.ps1"));

        // Assert
        validator.Should().Contain("Assert-ArchiveLayout $PrimaryArchiveX86 $true");
        validator.Should().Contain("Assert-ArchiveLayout $LegacyArchiveX64 $false");
        validator.Should().Contain("Mapping_Tools.Desktop.dll");
        validator.Should().Contain("Mapping Tools.dll");
        validator.Should().Contain("$InstallerX86");
        validator.Should().Contain("$InstallerX64");
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null &&
               !File.Exists(Path.Combine(directory.FullName, "Mapping_Tools.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not find Mapping_Tools.sln.");
    }
}
