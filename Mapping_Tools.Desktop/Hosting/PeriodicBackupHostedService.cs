using Mapping_Tools.Application.Backups;
using Mapping_Tools.Application.Backups.Contracts;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Workspace;
using Mapping_Tools.Application.Workspace.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Mapping_Tools.Desktop.Hosting;

internal sealed class PeriodicBackupHostedService : BackgroundService
{
    private static readonly TimeSpan minimumInterval = TimeSpan.FromSeconds(1);
    private readonly IBeatmapBackupService backupService;
    private readonly ICurrentBeatmapLocator currentBeatmapLocator;
    private readonly IBeatmapEditingGateway editingGateway;
    private readonly ILogger<PeriodicBackupHostedService> logger;
    private readonly ApplicationSettings settings;

    public PeriodicBackupHostedService(
        ApplicationSettings settings,
        ICurrentBeatmapLocator currentBeatmapLocator,
        IBeatmapEditingGateway editingGateway,
        IBeatmapBackupService backupService,
        ILogger<PeriodicBackupHostedService> logger)
    {
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.currentBeatmapLocator = currentBeatmapLocator
                                     ?? throw new ArgumentNullException(nameof(currentBeatmapLocator));
        this.editingGateway = editingGateway
                              ?? throw new ArgumentNullException(nameof(editingGateway));
        this.backupService = backupService
                             ?? throw new ArgumentNullException(nameof(backupService));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var interval = settings.PeriodicBackupInterval < minimumInterval
                ? minimumInterval
                : settings.PeriodicBackupInterval;
            await Task.Delay(interval, stoppingToken).ConfigureAwait(false);

            if (!settings.MakePeriodicBackups) continue;

            try
            {
                string? path = await currentBeatmapLocator
                    .FindCurrentBeatmapAsync(stoppingToken)
                    .ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(path)) continue;

                var session = await editingGateway
                    .OpenBeatmapAsync(
                        path,
                        LiveBeatmapPreference.RequireLive,
                        stoppingToken)
                    .ConfigureAwait(false);
                await backupService
                    .CreatePeriodicIfChangedAsync(session, stoppingToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    exception,
                    "Periodic beatmap backup could not be created.");
            }
        }
    }
}
