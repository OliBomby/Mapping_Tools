using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using Mapping_Tools.Application.Updates.Contracts;
using Mapping_Tools.Application.Updates.Models;
using Mapping_Tools.Application.Workspace.Contracts;
using Mapping_Tools.Application.Workspace.Models;
using Mapping_Tools.Desktop.Shell;

namespace Mapping_Tools.Desktop.ViewModels.GetStarted;

/// <summary>
///     Supplies onboarding, GitHub release notes, recent-map, and support-link content.
/// </summary>
public sealed partial class GetStartedViewModel : ObservableObject, IDisposable
{
    private readonly IBeatmapWorkspace workspace;
    private readonly IUiDispatcher dispatcher;
    private readonly IUpdateGateway updateGateway;
    private readonly CancellationTokenSource changelogCancellation = new();
    private bool disposed;

    /// <summary>Creates the landing-page presentation model.</summary>
    /// <param name="workspace">Supplies live recent history and accepts activated rows.</param>
    /// <param name="updateGateway">Fetches the GitHub release history.</param>
    /// <param name="dispatcher">Marshals fetched release notes onto the UI thread.</param>
    public GetStartedViewModel(
        IBeatmapWorkspace workspace,
        IUpdateGateway updateGateway,
        IUiDispatcher dispatcher)
    {
        this.workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        this.updateGateway = updateGateway ?? throw new ArgumentNullException(nameof(updateGateway));
        this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));

        RecentMaps = [];
        Changelog = [];
        RecentMaps.CollectionChanged += (_, _) =>
            HasNoRecentMaps = RecentMaps.Count == 0;
        this.workspace.SelectionChanged += OnWorkspaceSelectionChanged;
        RefreshRecentMaps();
        _ = LoadChangelogAsync();
    }

    /// <summary>Gets recent maps in persisted order.</summary>
    public ObservableCollection<RecentMapViewModel> RecentMaps { get; }

    /// <summary>Gets release notes fetched from GitHub in newest-first order.</summary>
    public ObservableCollection<ChangelogEntryViewModel> Changelog { get; }

    /// <summary>Gets whether an empty-state explanation should be shown.</summary>
    [ObservableProperty]
    public partial bool HasNoRecentMaps { get; private set; }

    /// <summary>Stops observing recent-history changes owned by the workspace.</summary>
    public void Dispose()
    {
        if (disposed) return;

        disposed = true;
        workspace.SelectionChanged -= OnWorkspaceSelectionChanged;
        changelogCancellation.Cancel();
        changelogCancellation.Dispose();
    }

    /// <summary>
    ///     Makes the selected recent rows the current beatmap selection in table order.
    /// </summary>
    /// <param name="recentMaps">Rows selected by the landing-page table.</param>
    public void SelectRecentMaps(IEnumerable<RecentMapViewModel> recentMaps)
    {
        ArgumentNullException.ThrowIfNull(recentMaps);
        string[] paths = recentMaps.Select(recent => recent.FullPath).ToArray();
        if (paths.Length > 0) workspace.SetSelection(paths, BeatmapSelectionSource.RecentHistory);
    }

    private void OnWorkspaceSelectionChanged(
        object? sender,
        BeatmapSelectionChangedEventArgs eventArgs)
    {
        RefreshRecentMaps();
    }

    private void RefreshRecentMaps()
    {
        RecentMaps.Clear();
        foreach (var recent in workspace.RecentMaps)
            RecentMaps.Add(new RecentMapViewModel(
                Path.GetFileName(recent.Path),
                recent.Path,
                recent.DisplayDate));

        HasNoRecentMaps = RecentMaps.Count == 0;
    }

    private async Task LoadChangelogAsync()
    {
        try
        {
            IReadOnlyList<UpdateReleaseNotes> notes = await updateGateway
                .GetReleaseNotesAsync(changelogCancellation.Token)
                .ConfigureAwait(false);
            if (disposed || notes.Count == 0) return;

            dispatcher.Post(() =>
            {
                if (disposed) return;

                foreach (UpdateReleaseNotes note in notes)
                {
                    if (string.IsNullOrWhiteSpace(note.Title) && string.IsNullOrWhiteSpace(note.Body)) continue;

                    Changelog.Add(new ChangelogEntryViewModel(
                        string.IsNullOrWhiteSpace(note.Title) ? "Release" : note.Title!,
                        note.Body ?? string.Empty));
                }
            });
        }
        catch (OperationCanceledException) when (changelogCancellation.IsCancellationRequested)
        {
            // Disposal is the expected cancellation path for the fire-and-forget load.
        }
        catch (HttpRequestException)
        {
            // The landing page remains usable when GitHub is unavailable.
        }
        catch (JsonException)
        {
            // The landing page remains usable when GitHub returns an invalid payload.
        }
    }
}
