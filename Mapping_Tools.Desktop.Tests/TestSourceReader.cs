using System.Reflection;

namespace Mapping_Tools.Desktop.Tests;

internal static class TestSourceReader
{
    public static string Read(string relativePath)
    {
        string? repositoryRoot = typeof(TestSourceReader).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => attribute.Key == "MappingToolsRepositoryRoot")
            ?.Value;

        DirectoryInfo? directory = repositoryRoot is null ? null : new DirectoryInfo(repositoryRoot);

        directory.Should().NotBeNull("the parity tests must run from the repository workspace");
        return File.ReadAllText(Path.Combine(directory!.FullName, relativePath));
    }
}
