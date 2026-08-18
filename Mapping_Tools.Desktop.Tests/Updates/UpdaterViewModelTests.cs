using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.Interactions;
using Mapping_Tools.Application.Updates;
using Mapping_Tools.Desktop.Shell;
using Mapping_Tools.Desktop.Updates;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Updates;

[TestClass]
public sealed class UpdaterViewModelTests
{
    [TestMethod]
    public void ProgressChanged_UpdatesDownloadProgressOnUiDispatcher()
    {
        // Arrange
        FakeUpdateService updates = new();
        UpdateCheckResult check = new(
            UpdateAvailability.Available,
            new Version(1, 0),
            new Version(2, 0),
            "Release",
            "Notes",
            "release.zip");
        using UpdaterViewModel viewModel = new(
            updates,
            check,
            new UserNotificationService(),
            new NoOpDialogService(),
            new ImmediateDispatcher());

        // Act
        updates.ReportProgress(0.5);

        // Assert
        viewModel.DownloadProgress.Should().Be(0.5);
        viewModel.ReleaseTitle.Should().Be("Release");
    }

    private sealed class ImmediateDispatcher : IUiDispatcher
    {
        public void Post(Action action) => action();
    }

    private sealed class NoOpDialogService : IDialogService
    {
        public Task<TResult> ShowMessageAsync<TResult>(
            MessageDialogRequest<TResult> request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(request.Choices.First().Result);

        public Task<ValueDialogResult<TResult>> ShowValueAsync<TResult>(
            ValueDialogRequest<TResult> request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new ValueDialogResult<TResult>(false, default));
    }

    private sealed class FakeUpdateService : IUpdateService
    {
        public event EventHandler<UpdateProgressChangedEventArgs>? ProgressChanged;

        public UpdateCheckResult? LastCheck => null;

        public Task? ActiveDownloadTask => null;

        public Task<UpdateCheckResult> CheckForUpdatesAsync(
            bool allowSkippedVersion,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task PrepareUpdateAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public void SkipCurrentVersion()
        {
        }

        public void StartUpdateProcess(bool restartAfterUpdate)
        {
        }

        public void AbandonUpdate()
        {
        }

        public void ReportProgress(double progress) =>
            ProgressChanged?.Invoke(
                this,
                new UpdateProgressChangedEventArgs(progress));

        public void Dispose()
        {
        }
    }
}
