using Mapping_Tools.ApplicationServices.BeatmapEditing;
using Mapping_Tools.ApplicationServices.Execution;
using Mapping_Tools.ApplicationServices.QuickRun;
using Mapping_Tools.ApplicationServices.Settings;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Infrastructure.Platform;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Platform.Tests;

[TestClass]
public sealed class QuickRunServiceTests
{
    [TestMethod]
    public void RegistryRejectsAmbiguityAndFiltersConfigurationChoices()
    {
        QuickRunCommandRegistry registry = new();
        registry.Register(Command("current", "Current", QuickRunTargets.Always));
        registry.Register(Command(
            "slider",
            "Slider",
            QuickRunTargets.AnySelection));

        Assert.ThrowsException<InvalidOperationException>(
            () => registry.Register(
                Command("current", "Other", QuickRunTargets.Always)));
        Assert.ThrowsException<InvalidOperationException>(
            () => registry.Register(
                Command("other", "Slider", QuickRunTargets.Always)));
        CollectionAssert.AreEqual(
            new[] { "current" },
            registry.GetCommandsFor(QuickRunTargets.NoSelection)
                .Select(command => command.Id)
                .ToArray());
        CollectionAssert.AreEqual(
            new[] { "current", "slider" },
            registry.GetCommandsFor(QuickRunTargets.SingleSelection)
                .Select(command => command.Id)
                .ToArray());
    }

    [TestMethod]
    public void RemovingCurrentCommandClearsSelection()
    {
        QuickRunCommandRegistry registry = new();
        registry.Register(Command("cleaner", "Map Cleaner", QuickRunTargets.Always));
        Assert.IsTrue(registry.SelectCurrent("cleaner"));

        Assert.IsTrue(registry.Remove("cleaner"));

        Assert.IsNull(registry.CurrentCommandId);
        Assert.IsFalse(registry.SelectCurrent("missing"));
    }

    [DataTestMethod]
    [DataRow(0, "none")]
    [DataRow(1, "single")]
    [DataRow(2, "multiple")]
    public async Task SmartRoutingUsesLiveSelectedHitObjectCount(
        int selectedCount,
        string expectedId)
    {
        List<string> invoked = [];
        QuickRunCommandRegistry registry = RegistryWithRoutingCommands(invoked);
        ApplicationSettings settings = new()
        {
            SmartQuickRunEnabled = true,
            NoneQuickRunTool = "None",
            SingleQuickRunTool = "Single",
            MultipleQuickRunTool = "Multiple"
        };
        QuickRunService service = CreateService(
            registry,
            new FakeLiveReader(Snapshot(selectedCount)),
            settings);

        QuickRunResult result = await service.RunAsync();

        Assert.AreEqual(QuickRunStatus.Executed, result.Status);
        Assert.AreEqual(expectedId, result.CommandId);
        CollectionAssert.AreEqual(new[] { expectedId }, invoked);
    }

    [TestMethod]
    public async Task DisabledSmartRoutingUsesCurrentCommandWithoutReadingEditor()
    {
        int invoked = 0;
        QuickRunCommandRegistry registry = new();
        registry.Register(
            new QuickRunCommand(
                "current",
                "Current",
                QuickRunTargets.Always,
                _ =>
                {
                    invoked++;
                    return Task.CompletedTask;
                }));
        registry.SelectCurrent("current");
        FakeLiveReader reader = new(
            new InvalidOperationException("Reader should not run."));
        QuickRunService service = CreateService(
            registry,
            reader,
            new ApplicationSettings { SmartQuickRunEnabled = false });

        QuickRunResult result = await service.RunAsync();

        Assert.AreEqual(QuickRunStatus.Executed, result.Status);
        Assert.AreEqual(1, invoked);
        Assert.AreEqual(0, reader.ReadCount);
    }

    [TestMethod]
    public async Task CurrentToolSentinelUsesCurrentCommandAfterSmartSelection()
    {
        int invoked = 0;
        QuickRunCommandRegistry registry = new();
        registry.Register(
            new QuickRunCommand(
                "active",
                "Active Tool",
                QuickRunTargets.NoSelection,
                _ =>
                {
                    invoked++;
                    return Task.CompletedTask;
                }));
        registry.SelectCurrent("active");
        QuickRunService service = CreateService(
            registry,
            new FakeLiveReader(Snapshot(3)),
            new ApplicationSettings
            {
                SmartQuickRunEnabled = true,
                MultipleQuickRunTool = "<Current Tool>"
            });

        QuickRunResult result = await service.RunAsync();

        Assert.AreEqual(QuickRunStatus.Executed, result.Status);
        Assert.AreEqual("active", result.CommandId);
        Assert.AreEqual(1, invoked);
    }

    [TestMethod]
    public async Task MissingEditorAndStaleToolReturnTypedOutcomesAndWarnings()
    {
        List<UserNotification> notifications = [];
        UserNotificationService notificationService = new();
        notificationService.Published +=
            (_, args) => notifications.Add(args.Notification);
        QuickRunCommandRegistry registry = new();
        QuickRunService unavailable = CreateService(
            registry,
            new FakeLiveReader((LiveBeatmapSnapshot?)null),
            new ApplicationSettings(),
            notificationService);

        QuickRunResult unavailableResult = await unavailable.RunAsync();

        Assert.AreEqual(QuickRunStatus.EditorUnavailable, unavailableResult.Status);
        QuickRunService stale = CreateService(
            registry,
            new FakeLiveReader(Snapshot(1)),
            new ApplicationSettings
            {
                SingleQuickRunTool = "Removed Tool"
            },
            notificationService);

        QuickRunResult staleResult = await stale.RunAsync();

        Assert.AreEqual(QuickRunStatus.CommandNotFound, staleResult.Status);
        Assert.AreEqual(2, notifications.Count);
        Assert.IsTrue(notifications.All(
            notification => notification.Severity == UserNotificationSeverity.Warning));
    }

    [TestMethod]
    public async Task ReaderAndCommandFailuresAreCapturedAndReported()
    {
        List<UserNotification> notifications = [];
        UserNotificationService notificationService = new();
        notificationService.Published +=
            (_, args) => notifications.Add(args.Notification);
        InvalidDataException readerFailure = new("Editor state is corrupt.");
        QuickRunService readerService = CreateService(
            new QuickRunCommandRegistry(),
            new FakeLiveReader(readerFailure),
            new ApplicationSettings(),
            notificationService);

        QuickRunResult readerResult = await readerService.RunAsync();

        Assert.AreEqual(QuickRunStatus.Failed, readerResult.Status);
        Assert.AreSame(readerFailure, readerResult.Exception);

        InvalidOperationException commandFailure = new("Tool validation failed.");
        QuickRunCommandRegistry registry = new();
        registry.Register(
            new QuickRunCommand(
                "tool",
                "Tool",
                QuickRunTargets.Always,
                _ => Task.FromException(commandFailure)));
        registry.SelectCurrent("tool");
        QuickRunService commandService = CreateService(
            registry,
            new FakeLiveReader((LiveBeatmapSnapshot?)null),
            new ApplicationSettings { SmartQuickRunEnabled = false },
            notificationService);

        QuickRunResult commandResult = await commandService.RunAsync();

        Assert.AreEqual(QuickRunStatus.Failed, commandResult.Status);
        Assert.AreSame(commandFailure, commandResult.Exception);
        Assert.AreEqual(2, notifications.Count);
        Assert.IsTrue(notifications.All(
            notification => notification.Severity == UserNotificationSeverity.Error));
    }

    [TestMethod]
    public async Task CallerCancellationIsNotConvertedIntoQuickRunFailure()
    {
        using CancellationTokenSource source = new();
        source.Cancel();
        QuickRunService service = CreateService(
            new QuickRunCommandRegistry(),
            new FakeLiveReader(Snapshot(0)),
            new ApplicationSettings());

        await Assert.ThrowsExceptionAsync<OperationCanceledException>(
            () => service.RunAsync(source.Token));
    }

    [TestMethod]
    public void WindowsAdapterTranslatesLegacyFixtureHotkeys()
    {
        Assert.AreEqual(
            0x4D,
            WindowsGlobalHotkeyService.ConvertLegacyKeyToVirtualKey(56));
        Assert.AreEqual(
            0x53,
            WindowsGlobalHotkeyService.ConvertLegacyKeyToVirtualKey(62));
        Assert.AreEqual(
            0x5A,
            WindowsGlobalHotkeyService.ConvertLegacyKeyToVirtualKey(69));
        Assert.ThrowsException<ArgumentOutOfRangeException>(
            () => WindowsGlobalHotkeyService.ConvertLegacyKeyToVirtualKey(-1));
    }

    private static QuickRunCommand Command(
        string id,
        string name,
        QuickRunTargets targets) =>
        new(id, name, targets, _ => Task.CompletedTask);

    private static QuickRunCommandRegistry RegistryWithRoutingCommands(
        ICollection<string> invoked)
    {
        QuickRunCommandRegistry registry = new();
        registry.Register(new QuickRunCommand(
            "none",
            "None",
            QuickRunTargets.NoSelection,
            _ => Invoke("none")));
        registry.Register(new QuickRunCommand(
            "single",
            "Single",
            QuickRunTargets.SingleSelection,
            _ => Invoke("single")));
        registry.Register(new QuickRunCommand(
            "multiple",
            "Multiple",
            QuickRunTargets.MultipleSelection,
            _ => Invoke("multiple")));
        return registry;

        Task Invoke(string id)
        {
            invoked.Add(id);
            return Task.CompletedTask;
        }
    }

    private static QuickRunService CreateService(
        QuickRunCommandRegistry registry,
        ILiveBeatmapReader reader,
        ApplicationSettings settings,
        IUserNotificationService? notifications = null)
    {
        return new QuickRunService(
            registry,
            reader,
            settings,
            notifications ?? new UserNotificationService());
    }

    private static LiveBeatmapSnapshot Snapshot(int selectedCount)
    {
        List<HitObject> hitObjects = [];
        for (int index = 0; index < Math.Max(3, selectedCount); index++)
        {
            hitObjects.Add(new HitObject
            {
                IsSelected = index < selectedCount
            });
        }

        return new LiveBeatmapSnapshot(
            @"C:\osu!\Songs\map.osu",
            [],
            [],
            hitObjects,
            -1,
            1.4,
            1);
    }

    private sealed class FakeLiveReader : ILiveBeatmapReader
    {
        private readonly LiveBeatmapSnapshot? _snapshot;
        private readonly Exception? _failure;

        public FakeLiveReader(LiveBeatmapSnapshot? snapshot)
        {
            _snapshot = snapshot;
        }

        public FakeLiveReader(Exception failure)
        {
            _failure = failure;
        }

        public int ReadCount { get; private set; }

        public Task<LiveBeatmapSnapshot?> ReadAsync(
            CancellationToken cancellationToken = default)
        {
            ReadCount++;
            cancellationToken.ThrowIfCancellationRequested();
            return _failure is null
                ? Task.FromResult(_snapshot)
                : Task.FromException<LiveBeatmapSnapshot?>(_failure);
        }
    }
}
