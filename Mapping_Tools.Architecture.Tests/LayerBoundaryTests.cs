using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Architecture.Tests;

[TestClass]
public sealed class LayerBoundaryTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    private static readonly string[] FrameworkNeutralProjects =
    [
        "Mapping_Tools.Core",
        "Mapping_Tools.Application"
    ];

    private static readonly string[] ForbiddenSourceTokens =
    [
        "System.Windows",
        "System.Windows.Forms",
        "Microsoft.Win32.OpenFileDialog",
        "Avalonia",
        "ReactiveUI",
        "MaterialDesignThemes",
        "System.Diagnostics.Process",
        "ProcessStartInfo",
        "OpenFileDialog",
        "SaveFileDialog",
        "FolderBrowserDialog",
        "CommonOpenFileDialog",
        "MessageBox"
    ];

    private static readonly string[] ForbiddenPackagePrefixes =
    [
        "Avalonia",
        "ReactiveUI",
        "Material.Avalonia",
        "MaterialDesign",
        "Microsoft-WindowsAPICodePack"
    ];

    [TestMethod]
    public void CoreAndApplicationDoNotContainFrontendOrProcessApis()
    {
        var violations = FrameworkNeutralProjects
            .SelectMany(project => Directory.EnumerateFiles(
                Path.Combine(RepositoryRoot, project), "*.cs", SearchOption.AllDirectories))
            .Where(path => !IsGeneratedPath(path))
            .SelectMany(path => FindTokenViolations(path, File.ReadAllText(path)))
            .ToArray();

        Assert.AreEqual(0, violations.Length, string.Join(Environment.NewLine, violations));
    }

    [TestMethod]
    public void CoreAndApplicationDoNotReferenceFrontendPackages()
    {
        var violations = FrameworkNeutralProjects
            .Select(project => Path.Combine(RepositoryRoot, project, $"{project}.csproj"))
            .SelectMany(FindForbiddenPackages)
            .ToArray();

        Assert.AreEqual(0, violations.Length, string.Join(Environment.NewLine, violations));
    }

    [TestMethod]
    public void ProjectReferencesFollowTheTargetDependencyDirection()
    {
        var expectedReferences = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["Mapping_Tools.Core"] = [],
            ["Mapping_Tools.Application"] = ["Mapping_Tools.Core"]
        };

        var violations = new List<string>();

        foreach (var (project, expected) in expectedReferences)
        {
            var projectPath = Path.Combine(RepositoryRoot, project, $"{project}.csproj");
            var actual = XDocument.Load(projectPath)
                .Descendants("ProjectReference")
                .Select(element => element.Attribute("Include")?.Value)
                .Where(value => value is not null)
                .Select(value => Path.GetFileNameWithoutExtension(value!))
                .Order(StringComparer.Ordinal)
                .ToArray();

            if (!actual.SequenceEqual(expected.Order(StringComparer.Ordinal)))
            {
                violations.Add($"{project}: expected [{string.Join(", ", expected)}], actual [{string.Join(", ", actual)}]");
            }
        }

        Assert.AreEqual(0, violations.Count, string.Join(Environment.NewLine, violations));
    }

    private static IEnumerable<string> FindTokenViolations(string path, string source)
    {
        foreach (var token in ForbiddenSourceTokens.Where(source.Contains))
        {
            yield return $"{Path.GetRelativePath(RepositoryRoot, path)} contains forbidden token '{token}'.";
        }
    }

    private static IEnumerable<string> FindForbiddenPackages(string projectPath)
    {
        var packages = XDocument.Load(projectPath)
            .Descendants("PackageReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(value => value is not null);

        foreach (var package in packages)
        {
            if (ForbiddenPackagePrefixes.Any(prefix => package!.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            {
                yield return $"{Path.GetRelativePath(RepositoryRoot, projectPath)} references forbidden package '{package}'.";
            }
        }
    }

    private static bool IsGeneratedPath(string path) =>
        path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
        path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase);

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Mapping_Tools.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not find Mapping_Tools.sln.");
    }
}
