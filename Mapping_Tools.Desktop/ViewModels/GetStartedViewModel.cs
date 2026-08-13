using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Mapping_Tools.Application.Workspace;

namespace Mapping_Tools.Desktop.ViewModels;

/// <summary>
/// Supplies offline onboarding, changelog, recent-map, and support-link content.
/// </summary>
public sealed partial class GetStartedViewModel : ObservableObject, IDisposable
{
    private readonly IBeatmapWorkspace _workspace;
    private bool _disposed;
    /// <summary>Creates the landing-page presentation model.</summary>
    /// <param name="workspace">Supplies live recent history and accepts activated rows.</param>
    public GetStartedViewModel(
        IBeatmapWorkspace workspace)
    {
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));

        RecentMaps = [];
        Changelog = [];
        RecentMaps.CollectionChanged += (_, _) =>
            HasNoRecentMaps = RecentMaps.Count == 0;
        _workspace.SelectionChanged += OnWorkspaceSelectionChanged;
        RefreshRecentMaps();
    }

    /// <summary>Gets recent maps in persisted order.</summary>
    public ObservableCollection<RecentMapViewModel> RecentMaps { get; }

    /// <summary>Gets bundled notes that are available offline.</summary>
    public IReadOnlyList<ChangelogEntryViewModel> Changelog { get; }

    /// <summary>Gets whether an empty-state explanation should be shown.</summary>
    [ObservableProperty]
    public partial bool HasNoRecentMaps { get; private set; }

    /// <summary>
    /// Makes the selected recent rows the current beatmap selection in table order.
    /// </summary>
    /// <param name="recentMaps">Rows selected by the landing-page table.</param>
    public void SelectRecentMaps(IEnumerable<RecentMapViewModel> recentMaps)
    {
        ArgumentNullException.ThrowIfNull(recentMaps);
        string[] paths = recentMaps.Select(recent => recent.FullPath).ToArray();
        if (paths.Length > 0)
        {
            _workspace.SetSelection(paths, BeatmapSelectionSource.RecentHistory);
        }
    }

    /// <summary>Stops observing recent-history changes owned by the workspace.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _workspace.SelectionChanged -= OnWorkspaceSelectionChanged;
    }

    private void OnWorkspaceSelectionChanged(
        object? sender,
        BeatmapSelectionChangedEventArgs eventArgs) =>
        RefreshRecentMaps();

    private void RefreshRecentMaps()
    {
        RecentMaps.Clear();
        foreach (RecentBeatmap recent in _workspace.RecentMaps)
        {
            RecentMaps.Add(new RecentMapViewModel(
                Path.GetFileName(recent.Path),
                recent.Path,
                recent.DisplayDate));
        }

        HasNoRecentMaps = RecentMaps.Count == 0;
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
