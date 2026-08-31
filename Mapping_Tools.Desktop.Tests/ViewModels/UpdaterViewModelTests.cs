using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Updates.Contracts;
using Mapping_Tools.Application.Updates.Models;
using Mapping_Tools.Desktop.Services.Dialogs;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UpdaterViewModel = Mapping_Tools.Desktop.ViewModels.UpdaterViewModel;

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
            new ImmediateTestDispatcher());

        // Act
        updates.ReportProgress(0.5);

        // Assert
        viewModel.DownloadProgress.Should().Be(0.5);
        viewModel.ReleaseTitle.Should().Be("Release");
    }

    private sealed class NoOpDialogService : IDialogService
    {
        public Task<TResult> ShowMessageAsync<TResult>(
            MessageDialogRequest<TResult> request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(request.Choices.First().Result);
        }

        public Task<ValueDialogResult<TResult>> ShowValueAsync<TResult>(
            ValueDialogRequest<TResult> request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ValueDialogResult<TResult>(false, default));
        }
    }

    private sealed class FakeUpdateService : IUpdateService
    {
        public event EventHandler<UpdateProgressChangedEventArgs>? ProgressChanged;

        public UpdateCheckResult? LastCheck => null;

        public Task? ActiveDownloadTask => null;

        public Task<UpdateCheckResult> CheckForUpdatesAsync(
            bool allowSkippedVersion,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task PrepareUpdateAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public void SkipCurrentVersion()
        {
        }

        public void StartUpdateProcess(bool restartAfterUpdate)
        {
        }

        public void AbandonUpdate()
        {
        }

        public void Dispose()
        {
        }

        public void ReportProgress(double progress)
        {
            ProgressChanged?.Invoke(
                this,
                new UpdateProgressChangedEventArgs(progress));
        }
    }
}
