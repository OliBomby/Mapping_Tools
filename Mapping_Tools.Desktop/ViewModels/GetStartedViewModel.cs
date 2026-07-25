using System.Collections.ObjectModel;
using Mapping_Tools.ApplicationServices.Execution;
using Mapping_Tools.ApplicationServices.Platform;
using Mapping_Tools.ApplicationServices.Settings;
using ReactiveUI;

namespace Mapping_Tools.Desktop.ViewModels;

/// <summary>
/// Supplies offline onboarding, changelog, recent-map, and support-link content.
/// </summary>
public sealed class GetStartedViewModel : ViewModelBase
{
    private static readonly Uri WebsiteUri = new("https://mappingtools.github.io");
    private static readonly Uri SourceUri = new("https://github.com/OliBomby/Mapping_Tools");
    private readonly IPlatformLauncher _launcher;
    private readonly IUserNotificationService _notifications;

    /// <summary>Creates the landing-page presentation model.</summary>
    public GetStartedViewModel(
        ApplicationSettings settings,
        IPlatformLauncher launcher,
        IUserNotificationService notifications)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _launcher = launcher ?? throw new ArgumentNullException(nameof(launcher));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));

        RecentMaps = new ObservableCollection<RecentMapViewModel>(
            settings.RecentMaps.Select(recent => new RecentMapViewModel(
                Path.GetFileName(recent.Path),
                recent.Path,
                recent.DisplayDate)));
        Changelog = [];
        OpenWebsiteCommand = ReactiveCommand.CreateFromTask(
            () => OpenUriAsync(WebsiteUri, "website"));
        OpenSourceCommand = ReactiveCommand.CreateFromTask(
            () => OpenUriAsync(SourceUri, "source repository"));
    }

    /// <summary>Gets recent maps in persisted order.</summary>
    public ObservableCollection<RecentMapViewModel> RecentMaps { get; }

    /// <summary>Gets bundled notes that are available offline.</summary>
    public IReadOnlyList<ChangelogEntryViewModel> Changelog { get; }

    /// <summary>Gets whether an empty-state explanation should be shown.</summary>
    public bool HasNoRecentMaps => RecentMaps.Count == 0;

    /// <summary>Gets the command that opens the Mapping Tools website.</summary>
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> OpenWebsiteCommand { get; }

    /// <summary>Gets the command that opens the source repository.</summary>
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> OpenSourceCommand { get; }

    private async Task OpenUriAsync(Uri uri, string destination)
    {
        bool accepted = await _launcher.OpenUriAsync(uri).ConfigureAwait(false);
        if (!accepted)
        {
            await _notifications.PublishAsync(new UserNotification(
                UserNotificationSeverity.Warning,
                "Could not open link",
                $"The {destination} could not be opened by the operating system.")).ConfigureAwait(false);
        }
    }
}

/// <summary>Displays one persisted recent-map entry without loading it.</summary>
/// <param name="FileName">Filename used in the compact list.</param>
/// <param name="FullPath">Complete path retained for a later open-map workflow.</param>
/// <param name="DisplayDate">Legacy-compatible timestamp text.</param>
public sealed record RecentMapViewModel(
    string FileName,
    string FullPath,
    string DisplayDate);

/// <summary>Displays one bundled offline release note.</summary>
/// <param name="Title">Release-note heading.</param>
/// <param name="Text">Release-note body.</param>
public sealed record ChangelogEntryViewModel(string Title, string Text);
