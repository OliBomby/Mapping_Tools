using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.Settings;
using Microsoft.Extensions.Hosting;

namespace Mapping_Tools.Desktop.Hosting;

internal sealed class BetterSaveOverrideHostedService : IHostedService
{
    private readonly IBetterSaveOverrideService _overrideService;
    private readonly ApplicationSettings _settings;

    public BetterSaveOverrideHostedService(
        IBetterSaveOverrideService overrideService,
        ApplicationSettings settings)
    {
        _overrideService = overrideService ?? throw new ArgumentNullException(nameof(overrideService));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _overrideService.Configure(_settings.SongsPath, _settings.OverrideOsuSave);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _overrideService.Stop();
        return Task.CompletedTask;
    }
}
