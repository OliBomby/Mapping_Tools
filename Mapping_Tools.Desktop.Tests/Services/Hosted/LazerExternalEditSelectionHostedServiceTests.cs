using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Workspace.Models;
using Mapping_Tools.Desktop.Services.Hosted;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Infrastructure.Editor;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Services.Hosted;

[TestClass]
public sealed class LazerExternalEditSelectionHostedServiceTests
{
    [TestMethod]
    public async Task ExecuteAsync_WhenMountAppearsAndDisappears_SelectsThenRestoresPreviousBeatmap()
    {
        // Arrange
        string root = Path.Combine(Path.GetTempPath(), $"MappingToolsLazerSelection-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        TestBeatmapWorkspace workspace = new();
        workspace.SetSelection(["previous.osu"]);
        ApplicationSettings settings = new();
        using LazerExternalEditSelectionHostedService sut = new(
            settings,
            new LazerExternalEditBeatmapLocator(root),
            workspace,
            new ImmediateTestDispatcher());

        try
        {
            // Act
            await sut.StartAsync(CancellationToken.None);
            string mount = Path.Combine(root, new string('d', 64));
            Directory.CreateDirectory(mount);
            string mountedPath = Path.Combine(mount, "external.osu");
            await File.WriteAllTextAsync(mountedPath, "osu file format v14");
            await WaitUntilAsync(() => workspace.SelectedPaths.SequenceEqual([mountedPath]));
            BeatmapSelectionSource? mountedSource = workspace.LastSelectionSource;
            Directory.Delete(mount, true);
            await WaitUntilAsync(() => workspace.SelectedPaths.SequenceEqual(["previous.osu"]));

            // Assert
            mountedSource.Should().Be(BeatmapSelectionSource.LazerExternalEdit);
            workspace.SelectedPaths.Should().Equal("previous.osu");
        }
        finally
        {
            await sut.StopAsync(CancellationToken.None);
            Directory.Delete(root, true);
        }
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(5));
        while (!condition())
            await Task.Delay(50, timeout.Token);
    }
}
