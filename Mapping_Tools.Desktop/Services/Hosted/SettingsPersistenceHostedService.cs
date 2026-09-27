using Mapping_Tools.Application.Settings.Contracts;
using Mapping_Tools.Desktop.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mapping_Tools.Desktop.Services.Hosted;

/// <summary>
///     Writes the shared settings document once during orderly application shutdown.
/// </summary>
public sealed class SettingsPersistenceHostedService : IHostedService
{
    private readonly DesktopApplicationSettings settings;
    private readonly ILogger<SettingsPersistenceHostedService> logger;
    private readonly ISettingsService settingsService;
    private bool saveOnShutdown = true;

    /// <summary>
    ///     Creates the process-lifetime persistence boundary for the shared settings instance.
    /// </summary>
    /// <param name="settings">The mutable settings document used by desktop services.</param>
    /// <param name="settingsService">The storage service invoked during host shutdown.</param>
    /// <param name="logger">Records settings persistence outcomes.</param>
    public SettingsPersistenceHostedService(
        DesktopApplicationSettings settings,
        ISettingsService settingsService,
        ILogger<SettingsPersistenceHostedService>? logger = null)
    {
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        this.logger = logger ?? NullLogger<SettingsPersistenceHostedService>.Instance;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (saveOnShutdown)
        {
            logger.LogInformation("Saving application settings on shutdown");
            settingsService.Save(settings);
            logger.LogInformation("Application settings saved");
        }
        else logger.LogInformation("Application settings save suppressed on shutdown");
        return Task.CompletedTask;
    }

    /// <summary>Prevents the current process from persisting settings during orderly shutdown.</summary>
    public void SuppressSave()
    {
        saveOnShutdown = false;
        logger.LogInformation("Application settings save suppressed by user");
    }
}
