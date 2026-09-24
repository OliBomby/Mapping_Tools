using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Workspace.Contracts;

namespace Mapping_Tools.Infrastructure.Editor.Mtipc2;

/// <summary>Finds osu!'s selected beatmap through MTIPC2, including song selection.</summary>
public sealed class Mtipc2CurrentBeatmapLocator : ICurrentBeatmapLocator
{
    private readonly ApplicationSettings settings;
    private readonly Mtipc2Client client;

    /// <summary>Creates a locator using osu!'s configured Songs path.</summary>
    public Mtipc2CurrentBeatmapLocator(ApplicationSettings settings, Mtipc2Client client)
    {
        this.settings = settings;
        this.client = client;
    }

    /// <inheritdoc />
    public Task<string> FindCurrentBeatmapAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string? id = client.CurrentBeatmap().FileId;
        if (id is null) throw new InvalidOperationException("osu! has no selected beatmap.");
        return Task.FromResult(Path.GetFullPath(Path.Combine(settings.SongsPath, id.Replace('/', Path.DirectorySeparatorChar))));
    }
}
