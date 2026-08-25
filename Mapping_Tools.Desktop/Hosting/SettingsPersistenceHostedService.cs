using Mapping_Tools.Application.Settings.Contracts;
using Mapping_Tools.Application.Settings.Models;
using Microsoft.Extensions.Hosting;

namespace Mapping_Tools.Desktop.Hosting;

/// <summary>
///     Writes the shared settings document once during orderly application shutdown.
/// </summary>
public sealed class SettingsPersistenceHostedService : IHostedService
{
    private readonly ApplicationSettings settings;
    private readonly ISettingsService settingsService;
    private bool saveOnShutdown = true;

    /// <summary>
    ///     Creates the process-lifetime persistence boundary for the shared settings instance.
    /// </summary>
    /// <param name="settings">The mutable settings document used by desktop services.</param>
    /// <param name="settingsService">The storage service invoked during host shutdown.</param>
    public SettingsPersistenceHostedService(
        ApplicationSettings settings,
        ISettingsService settingsService)
    {
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (saveOnShutdown) settingsService.Save(settings);
        return Task.CompletedTask;
    }

    /// <summary>Prevents the current process from persisting settings during orderly shutdown.</summary>
    public void SuppressSave()
    {
        saveOnShutdown = false;
    }
}
