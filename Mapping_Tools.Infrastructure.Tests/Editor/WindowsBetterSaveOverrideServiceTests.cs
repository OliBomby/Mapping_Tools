using System.Collections.Concurrent;
using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Execution.UserNotification.Models;
using Mapping_Tools.Application.Workspace.Contracts;
using Mapping_Tools.Infrastructure.Editor;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Infrastructure.Tests.Editor;

[TestClass]
public sealed class WindowsBetterSaveOverrideServiceTests
{
    [DataTestMethod]
    [DataRow("write")]
    [DataRow("create")]
    [DataRow("rename")]
    [DataRow("replace")]
    public async Task Configure_WithEditorSave_InvokesBetterSaveWithoutRepeatingOwnWrite(string saveKind)
    {
        // Arrange
        using SaveFixture fixture = new();
        if (saveKind == "create") File.Delete(fixture.MapPath);
        string temporaryPath = Path.Combine(fixture.DirectoryPath, "save.tmp");
        await File.WriteAllTextAsync(temporaryPath, "normal save");
        fixture.Service.Configure(fixture.DirectoryPath, true);

        // Act
        switch (saveKind)
        {
            case "rename":
                File.Move(temporaryPath, fixture.MapPath, true);
                break;
            case "replace":
                File.Replace(temporaryPath, fixture.MapPath, null);
                break;
            default:
                await File.WriteAllTextAsync(fixture.MapPath, "normal save");
                break;
        }
        await fixture.Saved.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Task.Delay(300);

        // Assert
        (await File.ReadAllTextAsync(fixture.MapPath)).Should().Be("BetterSave output");
        fixture.SaveCount.Should().Be(1);
        fixture.Notifications.Should().BeEmpty();
    }

    [TestMethod]
    public async Task Configure_WhileEditorStillHoldsFile_WaitsForCompletedSave()
    {
        // Arrange
        using SaveFixture fixture = new();
        fixture.Service.Configure(fixture.DirectoryPath, true);
        int savesWhileLocked;

        // Act
        await using (FileStream stream = new(fixture.MapPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
        {
            await stream.WriteAsync(new byte[] { 1, 2, 3 });
            await stream.FlushAsync();
            await Task.Delay(200);
            savesWhileLocked = fixture.SaveCount;
        }
        await fixture.Saved.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Assert
        savesWhileLocked.Should().Be(0);
        (await File.ReadAllTextAsync(fixture.MapPath)).Should().Be("BetterSave output");
        fixture.Notifications.Should().BeEmpty();
    }

    [TestMethod]
    public async Task Configure_WithSaveDuringUnrelatedLookup_RetainsCurrentMapEvent()
    {
        // Arrange
        using SaveFixture fixture = new();
        TaskCompletionSource lookupStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseLookup = new(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.FindCurrent = async cancellationToken =>
        {
            lookupStarted.TrySetResult();
            await releaseLookup.Task.WaitAsync(cancellationToken);
            return fixture.MapPath;
        };
        fixture.Service.Configure(fixture.DirectoryPath, true);

        // Act
        await File.WriteAllTextAsync(Path.Combine(fixture.DirectoryPath, "other.osu"), "unrelated save");
        await lookupStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await File.WriteAllTextAsync(fixture.MapPath, "normal save");
        await Task.Delay(150);
        releaseLookup.SetResult();
        await fixture.Saved.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Assert
        fixture.SaveCount.Should().Be(1);
        fixture.Notifications.Should().BeEmpty();
    }

    [DataTestMethod]
    [DataRow(false, true, true)]
    [DataRow(true, false, true)]
    [DataRow(true, true, false)]
    public async Task Configure_WithUnmatchedSave_DoesNotInvokeBetterSave(bool enabled, bool foreground, bool currentMap)
    {
        // Arrange
        using SaveFixture fixture = new();
        fixture.Foreground = foreground;
        fixture.Service.Configure(fixture.DirectoryPath, enabled);
        string path = currentMap ? fixture.MapPath : Path.Combine(fixture.DirectoryPath, "other.osu");

        // Act
        await File.WriteAllTextAsync(path, "normal save");
        await Task.Delay(300);

        // Assert
        fixture.SaveCount.Should().Be(0);
        fixture.Notifications.Should().BeEmpty();
    }

    [TestMethod]
    public async Task Stop_WithPendingSave_CancelsOverride()
    {
        // Arrange
        using SaveFixture fixture = new();
        TaskCompletionSource lookupStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.FindCurrent = async cancellationToken =>
        {
            lookupStarted.TrySetResult();
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return fixture.MapPath;
        };
        fixture.Service.Configure(fixture.DirectoryPath, true);
        await File.WriteAllTextAsync(fixture.MapPath, "normal save");
        await lookupStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Act
        fixture.Service.Stop();
        await Task.Delay(150);

        // Assert
        fixture.SaveCount.Should().Be(0);
        fixture.Notifications.Should().BeEmpty();
    }

    private sealed class SaveFixture : ICurrentBeatmapLocator, IBetterSaveService, IDisposable
    {
        private int saveCount;

        public SaveFixture()
        {
            DirectoryPath = Path.Combine(Path.GetTempPath(), $"BetterSaveTests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(DirectoryPath);
            MapPath = Path.Combine(DirectoryPath, "current.osu");
            File.WriteAllText(MapPath, "initial map");
            UserNotificationService notifications = new();
            notifications.Published += (_, args) => Notifications.Enqueue(args.Notification);
            Service = new WindowsBetterSaveOverrideService(this, this, notifications, () => true, () => Foreground);
        }

        public string DirectoryPath { get; }
        public string MapPath { get; }
        public bool Foreground { get; set; } = true;
        public int SaveCount => Volatile.Read(ref saveCount);
        public Func<CancellationToken, Task<string>>? FindCurrent { get; set; }
        public TaskCompletionSource Saved { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public ConcurrentQueue<UserNotification> Notifications { get; } = new();
        public WindowsBetterSaveOverrideService Service { get; }

        public Task<string> FindCurrentBeatmapAsync(CancellationToken cancellationToken = default)
        {
            return FindCurrent?.Invoke(cancellationToken) ?? Task.FromResult(MapPath);
        }

        public async Task<BetterSaveResult> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref saveCount);
            await File.WriteAllTextAsync(MapPath, "BetterSave output", cancellationToken);
            Saved.TrySetResult();
            return new BetterSaveResult(BetterSaveStatus.Saved, MapPath);
        }

        public void Dispose()
        {
            Service.Dispose();
            // Cancellation can still be unwinding an asynchronous hash read.
            for (int attempt = 0; ; attempt++)
            {
                try
                {
                    Directory.Delete(DirectoryPath, true);
                    return;
                }
                catch (IOException) when (attempt < 20)
                {
                    Thread.Sleep(20);
                }
            }
        }
    }
}
