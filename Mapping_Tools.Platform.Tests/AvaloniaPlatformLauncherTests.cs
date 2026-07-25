using Mapping_Tools.Desktop.Platform;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Platform.Tests;

[TestClass]
public sealed class AvaloniaPlatformLauncherTests
{
    [TestMethod]
    public async Task MissingTopLevelHasExplicitFailure()
    {
        AvaloniaPlatformLauncher launcher = new(() => null);

        await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            () => launcher.OpenUriAsync(new Uri("https://mappingtools.github.io")));
    }

    [TestMethod]
    public async Task LauncherRejectsRelativeUriBeforeAccessingPlatform()
    {
        bool accessed = false;
        AvaloniaPlatformLauncher launcher = new(() =>
        {
            accessed = true;
            return null;
        });

        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => launcher.OpenUriAsync(new Uri("relative", UriKind.Relative)));

        Assert.IsFalse(accessed);
    }

    [TestMethod]
    public async Task LauncherRejectsMissingFileBeforeAccessingPlatform()
    {
        bool accessed = false;
        AvaloniaPlatformLauncher launcher = new(() =>
        {
            accessed = true;
            return null;
        });
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".missing");

        await Assert.ThrowsExceptionAsync<FileNotFoundException>(
            () => launcher.OpenFileAsync(path));

        Assert.IsFalse(accessed);
    }

    [TestMethod]
    public async Task LauncherRejectsMissingFolderBeforeAccessingPlatform()
    {
        bool accessed = false;
        AvaloniaPlatformLauncher launcher = new(() =>
        {
            accessed = true;
            return null;
        });
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        await Assert.ThrowsExceptionAsync<DirectoryNotFoundException>(
            () => launcher.OpenFolderAsync(path));

        Assert.IsFalse(accessed);
    }
}
