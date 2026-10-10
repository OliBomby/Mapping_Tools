using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Desktop.Models;
using Microsoft.Extensions.Hosting;

namespace Mapping_Tools.Desktop.Services.Hosted;

internal sealed class BetterSaveOverrideHostedService : IHostedService
{
    private readonly IBetterSaveOverrideService overrideService;
    private readonly DesktopApplicationSettings settings;

    public BetterSaveOverrideHostedService(
        IBetterSaveOverrideService overrideService,
        DesktopApplicationSettings settings)
    {
        this.overrideService = overrideService ?? throw new ArgumentNullException(nameof(overrideService));
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        overrideService.Configure(settings.SongsPath, settings.OverrideOsuSave);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        overrideService.Stop();
        return Task.CompletedTask;
    }
}
