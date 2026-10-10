using System.Threading.Channels;
using Mapping_Tools.Core.Settings.Models;
using Mapping_Tools.Infrastructure.Platform;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Infrastructure.Tests.Platform;

[TestClass]
public sealed class PortalGlobalHotkeyServiceTests
{
    private static readonly TimeSpan timeout = TimeSpan.FromSeconds(5);

    [TestMethod]
    public async Task Start_WithConfiguredBindings_RegistersTogetherAndDispatchesById()
    {
        // Arrange
        RecordingPortal portal = new();
        PortalGlobalHotkeyService sut = CreateService(portal);
        TaskCompletionSource invoked = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int unrelatedCalls = 0;
        sut.SetBinding("quick-run", new HotkeySettings(44, 2), _ =>
        {
            invoked.TrySetResult();
            return Task.CompletedTask;
        });
        sut.SetBinding("quick-undo", new HotkeySettings(69, 2), _ =>
        {
            Interlocked.Increment(ref unrelatedCalls);
            return Task.CompletedTask;
        });

        try
        {
            // Act
            sut.Start();
            Registration registration = await portal.NextAsync();
            registration.Activated("unknown");
            registration.Activated("quick-run");
            await invoked.Task.WaitAsync(timeout);

            // Assert
            registration.Shortcuts.Should().BeEquivalentTo(new Dictionary<string, string?>
            {
                ["quick-run"] = "CTRL+a",
                ["quick-undo"] = "CTRL+z",
            });
            unrelatedCalls.Should().Be(0);
            registration.Updates.Should().BeEmpty();
        }
        finally
        {
            sut.Stop();
        }
    }

    [TestMethod]
    public async Task SetBinding_WithChangedHotkey_ClosesOldSessionBeforeBindingNewTrigger()
    {
        // Arrange
        RecordingPortal portal = new();
        PortalGlobalHotkeyService sut = CreateService(portal);
        sut.SetBinding("quick-run", new HotkeySettings(44, 2), _ => Task.CompletedTask);
        sut.Start();
        Registration previous = await portal.NextAsync();

        try
        {
            // Act
            sut.SetBinding("quick-run", new HotkeySettings(45, 3), _ => Task.CompletedTask);
            Registration replacement = await portal.NextAsync();

            // Assert
            previous.Exited.Task.IsCompletedSuccessfully.Should().BeTrue();
            replacement.Updates.Should().ContainSingle().Which.Should().Be(
                new KeyValuePair<string, HotkeySettings>("quick-run", new HotkeySettings(45, 3)));
            replacement.Shortcuts.Should().BeEquivalentTo(new Dictionary<string, string?>
            {
                ["quick-run"] = "CTRL+ALT+b",
            });
        }
        finally
        {
            sut.Stop();
        }
    }

    [TestMethod]
    public async Task SetBinding_WithSameHotkey_UpdatesCallbackWithoutAnotherPermissionDialog()
    {
        // Arrange
        RecordingPortal portal = new();
        PortalGlobalHotkeyService sut = CreateService(portal);
        int oldCalls = 0;
        TaskCompletionSource invoked = new(TaskCreationOptions.RunContinuationsAsynchronously);
        HotkeySettings hotkey = new(44, 2);
        sut.SetBinding("quick-run", hotkey, _ =>
        {
            Interlocked.Increment(ref oldCalls);
            return Task.CompletedTask;
        });
        sut.Start();
        Registration registration = await portal.NextAsync();

        try
        {
            // Act
            sut.SetBinding("quick-run", hotkey, _ =>
            {
                invoked.TrySetResult();
                return Task.CompletedTask;
            });
            registration.Activated("quick-run");
            await invoked.Task.WaitAsync(timeout);

            // Assert
            oldCalls.Should().Be(0);
            registration.Exited.Task.IsCompleted.Should().BeFalse();
            portal.HasPendingRegistration.Should().BeFalse();
        }
        finally
        {
            sut.Stop();
        }
    }

    [TestMethod]
    public async Task Stop_WithActiveSession_CancelsCallbacksAndIgnoresLateActivation()
    {
        // Arrange
        RecordingPortal portal = new();
        PortalGlobalHotkeyService sut = CreateService(portal);
        TaskCompletionSource<CancellationToken> invoked = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int calls = 0;
        sut.SetBinding("quick-run", new HotkeySettings(44, 2), token =>
        {
            Interlocked.Increment(ref calls);
            invoked.TrySetResult(token);
            return Task.CompletedTask;
        });
        sut.Start();
        Registration registration = await portal.NextAsync();
        registration.Activated("quick-run");
        CancellationToken callbackToken = await invoked.Task.WaitAsync(timeout);

        // Act
        sut.Stop();
        await registration.Exited.Task.WaitAsync(timeout);
        registration.Activated("quick-run");

        // Assert
        callbackToken.IsCancellationRequested.Should().BeTrue();
        calls.Should().Be(1);
    }

    [TestMethod]
    public async Task SetBinding_WithLastBindingDisabled_ClosesSessionAndClearsDesktopRegistration()
    {
        // Arrange
        RecordingPortal portal = new();
        PortalGlobalHotkeyService sut = CreateService(portal);
        sut.SetBinding("quick-run", new HotkeySettings(44, 2), _ => Task.CompletedTask);
        sut.Start();
        Registration registration = await portal.NextAsync();

        try
        {
            // Act
            sut.SetBinding("quick-run", new HotkeySettings(0, 0), _ => Task.CompletedTask);
            Registration replacement = await portal.NextAsync();
            await registration.Exited.Task.WaitAsync(timeout);

            // Assert
            replacement.Shortcuts.Should().BeEmpty();
            replacement.Updates.Should().ContainKey("quick-run");
        }
        finally
        {
            sut.Stop();
        }
    }

    [TestMethod]
    public async Task GetRegisteredShortcutsAsync_DesktopAssignmentChanged_ReadsActualAssignmentsWithoutRebinding()
    {
        // Arrange
        RecordingPortal portal = new();
        PortalGlobalHotkeyService sut = CreateService(portal);
        sut.SetBinding("quick-run", new HotkeySettings(44, 2), _ => Task.CompletedTask);
        sut.Start();
        Registration registration = await portal.NextAsync();

        try
        {
            // Act
            registration.Assignments["quick-run"] = new HotkeySettings(97, 8);
            var shortcuts = await sut.GetRegisteredShortcutsAsync(CancellationToken.None);

            // Assert
            shortcuts.Should().BeEquivalentTo(new Dictionary<string, HotkeySettings> { ["quick-run"] = new HotkeySettings(97, 8) });
            registration.ReadCount.Should().Be(1);
            registration.Exited.Task.IsCompleted.Should().BeFalse();
            portal.HasPendingRegistration.Should().BeFalse();
        }
        finally
        {
            sut.Stop();
        }
    }

    [TestMethod]
    public async Task GetRegisteredShortcutsAsync_SessionReplacedWhilePermissionPending_DiscardsOldAssignments()
    {
        // Arrange
        RecordingPortal portal = new() { RegisterAutomatically = false };
        PortalGlobalHotkeyService sut = CreateService(portal);
        sut.SetBinding("quick-run", new HotkeySettings(44, 2), _ => Task.CompletedTask);
        sut.Start();
        await portal.NextAsync();
        Task<Dictionary<string, HotkeySettings>> refresh = sut.GetRegisteredShortcutsAsync(CancellationToken.None);

        try
        {
            // Act
            sut.SetBinding("quick-run", new HotkeySettings(45, 2), _ => Task.CompletedTask);
            Func<Task> refreshOldSession = async () => await refresh.WaitAsync(timeout);

            // Assert
            await refreshOldSession.Should().ThrowAsync<OperationCanceledException>();
            (await portal.NextAsync()).Shortcuts.Should().ContainKey("quick-run");
        }
        finally
        {
            sut.Stop();
        }
    }

    [TestMethod]
    public async Task SetBinding_ActualDesktopGestureReentered_KeepsLivePortalIdAndAvoidsAnotherDialog()
    {
        // Arrange
        RecordingPortal portal = new();
        PortalGlobalHotkeyService sut = CreateService(portal);
        sut.SetBinding("quick-run", new HotkeySettings(44, 2), _ => Task.CompletedTask);
        sut.Start();
        Registration registration = await portal.NextAsync();
        registration.Assignments["quick-run"] = new HotkeySettings(97, 8);
        var actual = await sut.GetRegisteredShortcutsAsync(CancellationToken.None);
        TaskCompletionSource invoked = new(TaskCreationOptions.RunContinuationsAsynchronously);

        try
        {
            // Act
            sut.SetBinding("quick-run", actual["quick-run"], _ =>
            {
                invoked.TrySetResult();
                return Task.CompletedTask;
            });
            var refreshed = await sut.GetRegisteredShortcutsAsync(CancellationToken.None);
            registration.Activated("quick-run");
            await invoked.Task.WaitAsync(timeout);

            // Assert
            refreshed.Should().BeEquivalentTo(actual);
            registration.Exited.Task.IsCompleted.Should().BeFalse();
            portal.HasPendingRegistration.Should().BeFalse();
        }
        finally
        {
            sut.Stop();
        }
    }

    [TestMethod]
    public async Task GetRegisteredShortcutsAsync_UnassignedShortcut_WarnsOnceUntilAssignmentRecovers()
    {
        // Arrange
        RecordingPortal portal = new() { RegisterAutomatically = false };
        PortalGlobalHotkeyService sut = CreateService(portal);
        List<Exception> warnings = [];
        sut.RegistrationFailed += (_, exception) => warnings.Add(exception);
        sut.SetBinding("quick-run", new HotkeySettings(44, 2), _ => Task.CompletedTask);
        sut.Start();
        Registration registration = await portal.NextAsync();

        try
        {
            // Act
            registration.Assignments.Clear();
            registration.Register();
            var missing = await sut.GetRegisteredShortcutsAsync(CancellationToken.None);
            await sut.GetRegisteredShortcutsAsync(CancellationToken.None);
            int warningsBeforeRecovery = warnings.Count;
            registration.Assignments["quick-run"] = new HotkeySettings(44, 2);
            await sut.GetRegisteredShortcutsAsync(CancellationToken.None);
            registration.Assignments["quick-run"] = new HotkeySettings(0, 0);
            await sut.GetRegisteredShortcutsAsync(CancellationToken.None);

            // Assert
            missing.Should().BeEquivalentTo(new Dictionary<string, HotkeySettings> { ["quick-run"] = new HotkeySettings(0, 0) });
            warningsBeforeRecovery.Should().Be(1);
            warnings.Should().HaveCount(2);
        }
        finally
        {
            sut.Stop();
        }
    }

    [TestMethod]
    public async Task Start_DesktopRegistrationFails_ReportsOriginalExceptionAndClearsDisplayedAssignment()
    {
        // Arrange
        Exception failure = new InvalidOperationException("Desktop portal is unavailable");
        RecordingPortal portal = new() { Failure = failure };
        PortalGlobalHotkeyService sut = CreateService(portal);
        TaskCompletionSource<Exception> warning = new(TaskCreationOptions.RunContinuationsAsynchronously);
        sut.RegistrationFailed += (_, exception) => warning.TrySetResult(exception);
        sut.SetBinding("quick-run", new HotkeySettings(44, 2), _ => Task.CompletedTask);

        try
        {
            // Act
            sut.Start();
            Exception reported = await warning.Task.WaitAsync(timeout);
            var shortcuts = await sut.GetRegisteredShortcutsAsync(CancellationToken.None).WaitAsync(timeout);

            // Assert
            reported.Should().BeSameAs(failure);
            shortcuts.Should().BeEquivalentTo(new Dictionary<string, HotkeySettings> { ["quick-run"] = new HotkeySettings(0, 0) });
        }
        finally
        {
            sut.Stop();
        }
    }

    [TestMethod]
    public async Task GetRegisteredShortcutsAsync_DesktopQueryFails_ReportsFailureWithoutAnotherPermissionDialog()
    {
        // Arrange
        RecordingPortal portal = new();
        PortalGlobalHotkeyService sut = CreateService(portal);
        List<Exception> warnings = [];
        sut.RegistrationFailed += (_, exception) => warnings.Add(exception);
        sut.SetBinding("quick-run", new HotkeySettings(44, 2), _ => Task.CompletedTask);
        sut.Start();
        Registration registration = await portal.NextAsync();
        registration.ReadFailure = new IOException("Session disconnected");

        try
        {
            // Act
            var shortcuts = await sut.GetRegisteredShortcutsAsync(CancellationToken.None);

            // Assert
            shortcuts.Should().BeEquivalentTo(new Dictionary<string, HotkeySettings> { ["quick-run"] = new HotkeySettings(0, 0) });
            warnings.Should().ContainSingle().Which.Should().BeSameAs(registration.ReadFailure);
            portal.HasPendingRegistration.Should().BeFalse();
        }
        finally
        {
            sut.Stop();
        }
    }

    private static PortalGlobalHotkeyService CreateService(RecordingPortal portal)
    {
        return new PortalGlobalHotkeyService(portal, NullLogger<PortalGlobalHotkeyService>.Instance);
    }

    private sealed class RecordingPortal : IGlobalShortcutPortal
    {
        private readonly Channel<Registration> registrations = Channel.CreateUnbounded<Registration>();

        public bool HasPendingRegistration => registrations.Reader.TryPeek(out _);
        public bool RegisterAutomatically { get; set; } = true;
        public Exception? Failure { get; set; }

        public async Task RunAsync(
            IReadOnlyDictionary<string, string?> shortcuts,
            IReadOnlyDictionary<string, HotkeySettings> updates,
            Action<string> activated,
            Action<Dictionary<string, HotkeySettings>, Func<CancellationToken, Task<Dictionary<string, HotkeySettings>>>> registered,
            CancellationToken cancellationToken)
        {
            if (Failure is not null) throw Failure;
            Registration registration = new(shortcuts, updates, activated, registered);
            if (RegisterAutomatically) registration.Register();
            await registrations.Writer.WriteAsync(registration, cancellationToken);
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            finally
            {
                registration.Exited.TrySetResult();
            }
        }

        public async Task<Registration> NextAsync()
        {
            return await registrations.Reader.ReadAsync().AsTask().WaitAsync(timeout);
        }
    }

    private sealed record Registration(IReadOnlyDictionary<string, string?> Shortcuts,
        IReadOnlyDictionary<string, HotkeySettings> Updates, Action<string> Activated,
        Action<Dictionary<string, HotkeySettings>, Func<CancellationToken, Task<Dictionary<string, HotkeySettings>>>> Registered)
    {
        public TaskCompletionSource Exited { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Dictionary<string, HotkeySettings> Assignments { get; } = Shortcuts.ToDictionary(pair => pair.Key, pair => XdgShortcutTrigger.ParseDescription(pair.Value ?? "Ctrl+A"));
        public int ReadCount { get; private set; }
        public Exception? ReadFailure { get; set; }

        public void Register()
        {
            Registered(Assignments, _ =>
            {
                ReadCount++;
                if (ReadFailure is not null) throw ReadFailure;
                return Task.FromResult(new Dictionary<string, HotkeySettings>(Assignments));
            });
        }
    }
}
