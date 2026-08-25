using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Updates;
using Mapping_Tools.Application.Updates.Contracts;
using Mapping_Tools.Application.Updates.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.Updates;

[TestClass]
public sealed class UpdateServiceTests
{
    [TestMethod]
    public async Task CheckForUpdates_WhenLatestIsNotNewer_ReturnsNoUpdate()
    {
        // Arrange
        FakeUpdateGateway gateway = new(new UpdatePackageInfo(
            new Version(1, 2, 3),
            new Version(1, 2, 3),
            "Release",
            "Notes",
            "release.zip"));
        using UpdateService service = new(gateway, new ApplicationSettings());

        // Act
        var result = await service.CheckForUpdatesAsync(
            true);

        // Assert
        result.Availability.Should().Be(UpdateAvailability.None);
        result.CanUpdate.Should().BeFalse();
        result.ReleaseTitle.Should().Be("Release");
        result.ReleaseBody.Should().Be("Notes");
    }

    [TestMethod]
    public async Task CheckForUpdates_WithPersistedSkip_OnlySuppressesWhenPolicyAllowsIt()
    {
        // Arrange
        FakeUpdateGateway gateway = new(new UpdatePackageInfo(
            new Version(1, 0),
            new Version(2, 0),
            null,
            null,
            "release.zip"));
        ApplicationSettings settings = new() { SkipVersion = "2.0" };
        using UpdateService service = new(gateway, settings);

        // Act
        var startupResult = await service.CheckForUpdatesAsync(
            true);
        var manualResult = await service.CheckForUpdatesAsync(
            false);

        // Assert
        startupResult.Availability.Should().Be(UpdateAvailability.Skipped);
        manualResult.Availability.Should().Be(UpdateAvailability.Available);
    }

    [TestMethod]
    public async Task PrepareUpdate_AfterCheck_ReportsProgressAndLaunchesWithRequestedRestartMode()
    {
        // Arrange
        FakeUpdateGateway gateway = new(new UpdatePackageInfo(
            new Version(1, 0),
            new Version(2, 0),
            null,
            null,
            "release.zip"));
        using UpdateService service = new(gateway, new ApplicationSettings());
        List<double> progress = [];
        service.ProgressChanged += (_, args) => progress.Add(args.Progress);
        await service.CheckForUpdatesAsync(false);

        // Act
        await service.PrepareUpdateAsync();
        service.StartUpdateProcess(false);

        // Assert
        gateway.PreparedVersion.Should().Be(new Version(2, 0));
        gateway.LaunchedVersion.Should().Be(new Version(2, 0));
        gateway.RestartAfterUpdate.Should().BeFalse();
        progress.Should().Contain(1d);
    }

    [TestMethod]
    public async Task PrepareUpdate_BeforeCheck_ThrowsWithoutCallingGateway()
    {
        // Arrange
        FakeUpdateGateway gateway = new(new UpdatePackageInfo(
            new Version(1, 0),
            null,
            null,
            null,
            "release.zip"));
        using UpdateService service = new(gateway, new ApplicationSettings());

        // Act
        var act = () => service.PrepareUpdateAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        gateway.PreparedVersion.Should().BeNull();
    }

    [TestMethod]
    public async Task AbandonUpdate_CancelsInFlightPreparation()
    {
        // Arrange
        FakeUpdateGateway gateway = new(new UpdatePackageInfo(
            new Version(1, 0),
            new Version(2, 0),
            null,
            null,
            "release.zip"));
        TaskCompletionSource preparation = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        gateway.PrepareImplementation = (_, _, cancellationToken) =>
        {
            cancellationToken.Register(() =>
            {
                gateway.CancellationRequested = true;
                preparation.TrySetCanceled(cancellationToken);
            });
            return preparation.Task;
        };
        using UpdateService service = new(gateway, new ApplicationSettings());
        await service.CheckForUpdatesAsync(false);
        var download = service.PrepareUpdateAsync();

        // Act
        service.AbandonUpdate();

        // Assert
        var act = () => download;
        await act.Should().ThrowAsync<OperationCanceledException>();
        gateway.CancellationRequested.Should().BeTrue();
    }

    [TestMethod]
    public async Task DisposeAsync_WaitsForInFlightPreparationBeforeDisposingGateway()
    {
        // Arrange
        FakeUpdateGateway gateway = new(new UpdatePackageInfo(
            new Version(1, 0),
            new Version(2, 0),
            null,
            null,
            "release.zip"));
        TaskCompletionSource preparation = new(TaskCreationOptions.RunContinuationsAsynchronously);
        gateway.PrepareImplementation = (_, _, _) => preparation.Task;
        UpdateService service = new(gateway, new ApplicationSettings());
        await service.CheckForUpdatesAsync(false);
        _ = service.PrepareUpdateAsync();

        // Act
        var dispose = service.DisposeAsync().AsTask();

        // Assert
        dispose.IsCompleted.Should().BeFalse();
        gateway.Disposed.Should().BeFalse();
        preparation.SetResult();
        await dispose;
        gateway.Disposed.Should().BeTrue();
    }

    [TestMethod]
    public async Task PrepareUpdate_WhenRequestedConcurrently_ReusesOnePreparationTask()
    {
        // Arrange
        FakeUpdateGateway gateway = new(new UpdatePackageInfo(
            new Version(1, 0),
            new Version(2, 0),
            null,
            null,
            "release.zip"));
        TaskCompletionSource preparation = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        gateway.PrepareImplementation = (_, _, _) =>
        {
            gateway.PrepareCallCount++;
            return preparation.Task;
        };
        using UpdateService service = new(gateway, new ApplicationSettings());
        await service.CheckForUpdatesAsync(false);

        // Act
        var first = service.PrepareUpdateAsync();
        var second = service.PrepareUpdateAsync();
        preparation.SetResult();
        await first;
        await second;

        // Assert
        second.Should().BeSameAs(first);
        gateway.PrepareCallCount.Should().Be(1);
    }

    [TestMethod]
    public async Task CheckForUpdates_WhenPreparationIsActive_WaitsBeforeReusingGateway()
    {
        // Arrange
        FakeUpdateGateway gateway = new(new UpdatePackageInfo(
            new Version(1, 0),
            new Version(2, 0),
            null,
            null,
            "release.zip"));
        TaskCompletionSource preparation = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        gateway.PrepareImplementation = (_, _, _) => preparation.Task;
        using UpdateService service = new(gateway, new ApplicationSettings());
        await service.CheckForUpdatesAsync(false);
        var download = service.PrepareUpdateAsync();

        // Act
        var check = service.CheckForUpdatesAsync(
            false);

        // Assert
        gateway.CheckCallCount.Should().Be(1);
        check.IsCompleted.Should().BeFalse();

        preparation.SetResult();
        var awaitDownload = () => download;
        await awaitDownload.Should().ThrowAsync<OperationCanceledException>();
        await check;
        gateway.CheckCallCount.Should().Be(2);
    }

    private sealed class FakeUpdateGateway(UpdatePackageInfo result) : IUpdateGateway
    {
        public UpdatePackageInfo Result { get; } = result;

        public Version? PreparedVersion { get; private set; }

        public int PrepareCallCount { get; set; }

        public int CheckCallCount { get; private set; }

        public Version? LaunchedVersion { get; private set; }

        public bool RestartAfterUpdate { get; private set; }

        public bool CancellationRequested { get; set; }

        public bool Disposed { get; private set; }

        public Func<Version, IProgress<double>, CancellationToken, Task>? PrepareImplementation { get; set; }

        public Task<UpdatePackageInfo> CheckForUpdatesAsync(
            CancellationToken cancellationToken = default)
        {
            return CheckAsync(cancellationToken);
        }

        public Task PrepareUpdateAsync(
            Version version,
            IProgress<double> progress,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (PrepareImplementation is not null) return PrepareImplementation(version, progress, cancellationToken);

            PreparedVersion = version;
            progress.Report(1d);
            return Task.CompletedTask;
        }

        public void LaunchUpdater(Version version, bool restartAfterUpdate)
        {
            LaunchedVersion = version;
            RestartAfterUpdate = restartAfterUpdate;
        }

        public void Dispose()
        {
            Disposed = true;
        }

        private Task<UpdatePackageInfo> CheckAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CheckCallCount++;
            return Task.FromResult(Result);
        }
    }
}
