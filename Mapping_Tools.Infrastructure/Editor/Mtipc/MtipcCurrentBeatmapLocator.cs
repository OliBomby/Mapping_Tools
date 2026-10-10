using System.Net.Sockets;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Workspace.Contracts;

namespace Mapping_Tools.Infrastructure.Editor.Mtipc;

/// <summary>Locates osu!'s current beatmap through MTIPC.</summary>
public sealed class MtipcCurrentBeatmapLocator : ICurrentBeatmapLocator
{
    private readonly MtipcClient client = new();
    private readonly Func<string> songsPathProvider;

    /// <summary>Initializes an MTIPC current-beatmap locator.</summary>
    /// <param name="songsPath">The osu! Songs directory used to build the full path.</param>
    public MtipcCurrentBeatmapLocator(string songsPath)
    {
        songsPathProvider = () => songsPath;
    }

    /// <summary>Initializes an MTIPC locator using the live application settings.</summary>
    /// <param name="settings">Settings containing the osu! Songs directory.</param>
    public MtipcCurrentBeatmapLocator(ApplicationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        songsPathProvider = () => settings.SongsPath;
    }

    /// <inheritdoc />
    public async Task<string> FindCurrentBeatmapAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await Task.Run(() =>
            {
                string songsPath = songsPathProvider();
                MtipcBeatmapData beatmap = client.ReadBeatmap(cancellationToken);
                if (string.IsNullOrWhiteSpace(songsPath)
                    || string.IsNullOrWhiteSpace(beatmap.ContainingFolder)
                    || string.IsNullOrWhiteSpace(beatmap.Filename))
                    throw new InvalidOperationException("MTIPC returned no current beatmap.");

                return Path.GetFullPath(Path.Combine(songsPath, beatmap.ContainingFolder, beatmap.Filename));
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or SocketException)
        {
            throw new InvalidOperationException(
                "Could not determine the beatmap currently open in osu! through MTIPC.",
                exception);
        }
    }
}
