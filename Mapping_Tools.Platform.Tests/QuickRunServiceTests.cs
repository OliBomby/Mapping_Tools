using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.QuickRun;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Infrastructure.Platform;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Platform.Tests;

[TestClass]
public sealed class QuickRunServiceTests
{
    [TestMethod]
    public void Register_WithAmbiguousCommands_ThrowsAndFiltersChoices()
    {
        // Arrange
        QuickRunCommandRegistry registry = new();
        registry.Register(Command("current", "Current", QuickRunTargets.Always));
        registry.Register(Command(
            "slider",
            "Slider",
            QuickRunTargets.AnySelection));

        // Act
        Action act1 = () => registry.Register(
                Command("current", "Other", QuickRunTargets.Always));

        // Assert
        act1.Should().Throw<InvalidOperationException>();
        Action act2 = () => registry.Register(
                Command("other", "Slider", QuickRunTargets.Always));

        act2.Should().Throw<InvalidOperationException>();
        registry.GetCommandsFor(QuickRunTargets.NoSelection)
            .Select(command => command.Id)
            .ToArray().Should().Equal(new[] { "current" });
        registry.GetCommandsFor(QuickRunTargets.SingleSelection)
            .Select(command => command.Id)
            .ToArray().Should().Equal(new[] { "current", "slider" });
    }

    [TestMethod]
    public void Remove_WithCurrentCommand_ClearsSelection()
    {
        // Arrange
        // Act
        QuickRunCommandRegistry registry = new();
        registry.Register(Command("cleaner", "Map Cleaner", QuickRunTargets.Always));
        // Assert
        registry.SelectCurrent("cleaner").Should().BeTrue();

        registry.Remove("cleaner").Should().BeTrue();

        registry.CurrentCommandId.Should().BeNull();
        registry.SelectCurrent("missing").Should().BeFalse();
    }

    [DataTestMethod]
    [DataRow(0, "none")]
    [DataRow(1, "single")]
    [DataRow(2, "multiple")]
    public async Task RunAsync_WithSelectedHitObjectCount_SelectsExpectedCommand(
        int selectedCount,
        string expectedId)
    {
        // Arrange
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

        // Act
        QuickRunResult result = await service.RunAsync();

        // Assert
        result.Status.Should().Be(QuickRunStatus.Executed);
        result.CommandId.Should().Be(expectedId);
        invoked.Should().Equal(new[] { expectedId });
    }

    [TestMethod]
    public async Task RunAsync_WithSmartRoutingDisabled_UsesCurrentWithoutReader()
    {
        // Arrange
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

        // Act
        QuickRunResult result = await service.RunAsync();

        // Assert
        result.Status.Should().Be(QuickRunStatus.Executed);
        invoked.Should().Be(1);
        reader.ReadCount.Should().Be(0);
    }

    [TestMethod]
    public async Task RunAsync_WithCurrentToolSentinel_UsesCurrentAfterSmartSelection()
    {
        // Arrange
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

        // Act
        QuickRunResult result = await service.RunAsync();

        // Assert
        result.Status.Should().Be(QuickRunStatus.Executed);
        result.CommandId.Should().Be("active");
        invoked.Should().Be(1);
    }

    [TestMethod]
    public async Task RunAsync_WithMissingEditorOrStaleTool_ReturnsTypedWarnings()
    {
        // Arrange
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

        // Act
        QuickRunResult unavailableResult = await unavailable.RunAsync();

        // Assert
        unavailableResult.Status.Should().Be(QuickRunStatus.EditorUnavailable);
        QuickRunService stale = CreateService(
            registry,
            new FakeLiveReader(Snapshot(1)),
            new ApplicationSettings
            {
                SingleQuickRunTool = "Removed Tool"
            },
            notificationService);

        QuickRunResult staleResult = await stale.RunAsync();

        staleResult.Status.Should().Be(QuickRunStatus.CommandNotFound);
        notifications.Count.Should().Be(2);
        notifications.All(
            notification => notification.Severity == UserNotificationSeverity.Warning).Should().BeTrue();
    }

    [TestMethod]
    public async Task RunAsync_WhenReaderOrCommandFails_ReturnsReportedFailure()
    {
        // Arrange
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

        // Act
        QuickRunResult readerResult = await readerService.RunAsync();

        // Assert
        readerResult.Status.Should().Be(QuickRunStatus.Failed);
        readerResult.Exception.Should().BeSameAs(readerFailure);

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

        commandResult.Status.Should().Be(QuickRunStatus.Failed);
        commandResult.Exception.Should().BeSameAs(commandFailure);
        notifications.Count.Should().Be(2);
        notifications.All(
            notification => notification.Severity == UserNotificationSeverity.Error).Should().BeTrue();
    }

    [TestMethod]
    public async Task RunAsync_WithCallerCancellation_PropagatesCancellation()
    {
        // Arrange
        using CancellationTokenSource source = new();
        source.Cancel();
        QuickRunService service = CreateService(
            new QuickRunCommandRegistry(),
            new FakeLiveReader(Snapshot(0)),
            new ApplicationSettings());

        // Act
        Func<Task> act3 = () => service.RunAsync(source.Token);

        // Assert
        await act3.Should().ThrowAsync<OperationCanceledException>();
    }

    [TestMethod]
    public void ConvertLegacyKeyToVirtualKey_WithLegacyHotkeys_MapsValuesAndRejectsInvalid()
    {
        // Arrange
        // Act
        // Assert
        WindowsGlobalHotkeyService.ConvertLegacyKeyToVirtualKey(56).Should().Be(0x4D);
        WindowsGlobalHotkeyService.ConvertLegacyKeyToVirtualKey(62).Should().Be(0x53);
        WindowsGlobalHotkeyService.ConvertLegacyKeyToVirtualKey(69).Should().Be(0x5A);
        Action act4 = () => WindowsGlobalHotkeyService.ConvertLegacyKeyToVirtualKey(-1);

        act4.Should().Throw<ArgumentOutOfRangeException>();
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
