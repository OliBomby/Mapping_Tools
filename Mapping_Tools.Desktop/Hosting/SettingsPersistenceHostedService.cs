using Mapping_Tools.Application.Settings;
using Microsoft.Extensions.Hosting;

namespace Mapping_Tools.Desktop.Hosting;

/// <summary>
/// Writes the shared settings document once during orderly application shutdown.
/// </summary>
public sealed class SettingsPersistenceHostedService : IHostedService
{
    private readonly ApplicationSettings _settings;
    private readonly ISettingsService _settingsService;

    /// <summary>
    /// Creates the process-lifetime persistence boundary for the shared settings instance.
    /// </summary>
    /// <param name="settings">The mutable settings document used by desktop services.</param>
    /// <param name="settingsService">The storage service invoked during host shutdown.</param>
    public SettingsPersistenceHostedService(
        ApplicationSettings settings,
        ISettingsService settingsService)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
    }

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken) =>
        Task.CompletedTask;

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _settingsService.Save(_settings);
        return Task.CompletedTask;
    }
}
