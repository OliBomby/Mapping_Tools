using Mapping_Tools.Infrastructure.Projects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Infrastructure.Tests.Projects;

[TestClass]
public sealed class FileSystemProjectStoreTests
{
    [TestMethod]
    public async Task SaveDocumentAsync_ExistingFileWithoutOverwrite_PreservesTheOriginalDocument()
    {
        // Arrange
        string directory = Path.Combine(Path.GetTempPath(), "mapping-tools-project-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "project.json");
        const string original = "{\"CliTargets\":[\"original.osu\"]}";
        await File.WriteAllTextAsync(path, original);
        FileSystemProjectStore store = new(new VersionedProjectJsonSerializer());

        try
        {
            // Act
            Func<Task> act = () => store.SaveDocumentAsync(path, "{}", false);

            // Assert
            await act.Should().ThrowAsync<IOException>();
            (await store.LoadDocumentAsync(path)).Should().Be(original);
            Directory.EnumerateFiles(directory).Should().ContainSingle();
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }
}
