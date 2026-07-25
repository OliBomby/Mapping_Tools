using Mapping_Tools.Infrastructure.Files;
using Mapping_Tools.Infrastructure.Platform;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Platform.Tests;

[TestClass]
public sealed class DesktopPlatformAdapterTests
{
    [TestMethod]
    public void ApplicationDirectoriesPreserveLegacyLayout()
    {
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        ApplicationDirectories directories = new(root);

        Assert.AreEqual(
            Path.Combine(Path.GetFullPath(root), "Mapping Tools"),
            directories.ApplicationData);
        Assert.AreEqual(
            Path.Combine(Path.GetFullPath(root), "Mapping Tools", "Exports"),
            directories.Exports);
    }

    [TestMethod]
    public void ApplicationDirectoriesCreateBothFolders()
    {
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        ApplicationDirectories directories = new(root);

        try
        {
            directories.EnsureCreated();

            Assert.IsTrue(Directory.Exists(directories.ApplicationData));
            Assert.IsTrue(Directory.Exists(directories.Exports));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [TestMethod]
    public async Task RevealRejectsMissingPathWithoutStartingExplorer()
    {
        WindowsFileRevealService reveal = new();
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".missing");

        await Assert.ThrowsExceptionAsync<FileNotFoundException>(
            () => reveal.RevealAsync(path));
    }
}
