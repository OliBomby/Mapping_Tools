using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Mapping_Tools.Application.Settings;

namespace Mapping_Tools.Desktop.ViewModels;

/// <summary>
/// Supplies offline onboarding, changelog, recent-map, and support-link content.
/// </summary>
public sealed partial class GetStartedViewModel : ObservableObject
{
    private static readonly string[] OnboardingInstructions =
    [
        "0. Set the correct path to your Songs folder in the [Preferences].",
        "1. Select a beatmap [File] -> [Open beatmap/Open current beatmap] to select a file from your system OR the current in-game selected beatmap.",
        "2. Select a tool that you want to use from the navigation menu. (Ctrl+K)",
        "3. Read a basic summary of the tool by clicking the (?) button.",
        "4. Configure your tool. To find out what specific things do, read the tooltips by hovering over them.",
        "5. Click the run button in the bottom right to run the program.",
        "6. Reload your beatmap WITHOUT SAVING by either leaving and re-entering the editor or pressing Ctrl+L, Enter.",
        "7. If you run into issues, consult the FAQ over on the [About] -> [Website]."
    ];
    /// <summary>Creates the landing-page presentation model.</summary>
    public GetStartedViewModel(
        ApplicationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        RecentMaps = new ObservableCollection<RecentMapViewModel>(
            settings.RecentMaps.Select(recent => new RecentMapViewModel(
                Path.GetFileName(recent.Path),
                recent.Path,
                recent.DisplayDate)));
        Changelog = [];
        Instructions = OnboardingInstructions;
        HasNoRecentMaps = RecentMaps.Count == 0;
        RecentMaps.CollectionChanged += (_, _) =>
            HasNoRecentMaps = RecentMaps.Count == 0;
    }

    /// <summary>Gets recent maps in persisted order.</summary>
    public ObservableCollection<RecentMapViewModel> RecentMaps { get; }

    /// <summary>Gets the ordered legacy onboarding instructions.</summary>
    public IReadOnlyList<string> Instructions { get; }

    /// <summary>Gets bundled notes that are available offline.</summary>
    public IReadOnlyList<ChangelogEntryViewModel> Changelog { get; }

    /// <summary>Gets whether an empty-state explanation should be shown.</summary>
    [ObservableProperty]
    public partial bool HasNoRecentMaps { get; private set; }

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
