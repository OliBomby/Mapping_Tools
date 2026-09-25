using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Workspace.Contracts;
using Mapping_Tools.Application.Workspace.Models;
using Mapping_Tools.Desktop.Shell;
using Mapping_Tools.Infrastructure.Editor;
using Microsoft.Extensions.Hosting;

namespace Mapping_Tools.Desktop.Services.Hosted;

internal sealed class LazerExternalEditSelectionHostedService : BackgroundService
{
    private readonly IUiDispatcher dispatcher;
    private readonly LazerExternalEditBeatmapLocator locator;
    private readonly ApplicationSettings settings;
    private readonly IBeatmapWorkspace workspace;
    private string? mountedPath;
    private string[] previousSelection = [];

    public LazerExternalEditSelectionHostedService(
        ApplicationSettings settings,
        LazerExternalEditBeatmapLocator locator,
        IBeatmapWorkspace workspace,
        IUiDispatcher dispatcher)
    {
        this.settings = settings;
        this.locator = locator;
        this.workspace = workspace;
        this.dispatcher = dispatcher;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        workspace.SelectionChanged += OnSelectionChanged;
        return base.StartAsync(cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        workspace.SelectionChanged -= OnSelectionChanged;
        await base.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            string? path = settings.AutoDetectLazerExternalEdit
                ? locator.FindMountedBeatmap()
                : null;
            dispatcher.Post(() => ApplySelection(path));

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private void OnSelectionChanged(object? sender, BeatmapSelectionChangedEventArgs eventArgs)
    {
        if (eventArgs.Source != BeatmapSelectionSource.LazerExternalEdit
            && eventArgs.Paths.Count == 1)
            locator.Prefer(eventArgs.Paths[0]);
    }

    private void ApplySelection(string? path)
    {
        if (path is not null)
        {
            if (mountedPath is null)
                previousSelection = workspace.SelectedPaths.ToArray();

            bool stillUsingMount = mountedPath is null
                                   || workspace.SelectedPaths.Any(selected =>
                                       string.Equals(
                                           Path.GetDirectoryName(selected),
                                           Path.GetDirectoryName(mountedPath),
                                           StringComparison.OrdinalIgnoreCase));

            mountedPath = path;
            if (stillUsingMount
                && !workspace.SelectedPaths.SequenceEqual([path]))
                workspace.SetSelection([path], BeatmapSelectionSource.LazerExternalEdit);

            return;
        }

        if (mountedPath is null) return;

        if (workspace.SelectedPaths.Any(selected =>
                string.Equals(
                    Path.GetDirectoryName(selected),
                    Path.GetDirectoryName(mountedPath),
                    StringComparison.OrdinalIgnoreCase)))
        {
            if (previousSelection.Length > 0)
                workspace.SetSelection(previousSelection);
            else
                workspace.ClearSelection();
        }

        mountedPath = null;
        previousSelection = [];
    }
}
