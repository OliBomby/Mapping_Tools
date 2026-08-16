using Mapping_Tools.Application.Backups;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Application.Workspace;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Mapping_Tools.Desktop.Hosting;

internal sealed class PeriodicBackupHostedService : BackgroundService
{
    private static readonly TimeSpan MinimumInterval = TimeSpan.FromSeconds(1);
    private readonly ApplicationSettings _settings;
    private readonly ICurrentBeatmapLocator _currentBeatmapLocator;
    private readonly IBeatmapEditingGateway _editingGateway;
    private readonly IBeatmapBackupService _backupService;
    private readonly ILogger<PeriodicBackupHostedService> _logger;

    public PeriodicBackupHostedService(
        ApplicationSettings settings,
        ICurrentBeatmapLocator currentBeatmapLocator,
        IBeatmapEditingGateway editingGateway,
        IBeatmapBackupService backupService,
        ILogger<PeriodicBackupHostedService> logger)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _currentBeatmapLocator = currentBeatmapLocator
            ?? throw new ArgumentNullException(nameof(currentBeatmapLocator));
        _editingGateway = editingGateway
            ?? throw new ArgumentNullException(nameof(editingGateway));
        _backupService = backupService
            ?? throw new ArgumentNullException(nameof(backupService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            TimeSpan interval = _settings.PeriodicBackupInterval < MinimumInterval
                ? MinimumInterval
                : _settings.PeriodicBackupInterval;
            await Task.Delay(interval, stoppingToken).ConfigureAwait(false);

            if (!_settings.MakePeriodicBackups)
            {
                continue;
            }

            try
            {
                string? path = await _currentBeatmapLocator
                    .FindCurrentBeatmapAsync(stoppingToken)
                    .ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                BeatmapEditingSession session = await _editingGateway
                    .OpenBeatmapAsync(
                        path,
                        LiveBeatmapPreference.RequireLive,
                        stoppingToken)
                    .ConfigureAwait(false);
                await _backupService
                    .CreatePeriodicIfChangedAsync(session, stoppingToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Periodic beatmap backup could not be created.");
            }
        }
    }
}
