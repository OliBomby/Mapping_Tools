using System.Text.Json;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Workspace.Contracts;

namespace Mapping_Tools.Infrastructure.Editor;

/// <summary>Locates osu!'s selected beatmap through the local gosumemory JSON API.</summary>
public sealed class GosumemoryCurrentBeatmapLocator : ICurrentBeatmapLocator
{
    private static readonly Uri endpoint = new("http://127.0.0.1:24050/json");
    private static readonly TimeSpan requestTimeout = TimeSpan.FromSeconds(3);

    private readonly HttpClient httpClient;
    private readonly ApplicationSettings settings;

    /// <summary>Initializes a gosumemory-backed current-beatmap locator.</summary>
    /// <param name="httpClient">Sends requests to the local gosumemory API.</param>
    /// <param name="settings">Settings containing osu!'s Songs directory.</param>
    public GosumemoryCurrentBeatmapLocator(HttpClient httpClient, ApplicationSettings settings)
    {
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <inheritdoc />
    public async Task<string> FindCurrentBeatmapAsync(CancellationToken cancellationToken = default)
    {
        using CancellationTokenSource timeout =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(requestTimeout);

        try
        {
            await using Stream response = await httpClient
                .GetStreamAsync(endpoint, timeout.Token)
                .ConfigureAwait(false);
            using JsonDocument document = await JsonDocument
                .ParseAsync(response, cancellationToken: timeout.Token)
                .ConfigureAwait(false);

            string? folder = GetNestedString(document.RootElement, "menu", "bm", "path", "folder");
            string? filename = GetNestedString(document.RootElement, "menu", "bm", "path", "file");
            if (string.IsNullOrWhiteSpace(folder) || string.IsNullOrWhiteSpace(filename))
                throw new InvalidOperationException(
                    "Gosumemory did not report a selected beatmap. Open a beatmap in osu! and try again.");

            if (string.IsNullOrWhiteSpace(settings.SongsPath))
                throw new InvalidOperationException(
                    "Set the osu! Songs folder in Mapping Tools preferences before using gosumemory.");

            string songsPath = Path.GetFullPath(settings.SongsPath);
            string beatmapPath = Path.GetFullPath(Path.Combine(songsPath, folder, filename));
            string relativePath = Path.GetRelativePath(songsPath, beatmapPath);
            if (Path.IsPathRooted(relativePath)
                || relativePath == ".."
                || relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || relativePath.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Gosumemory reported a beatmap path outside the configured osu! Songs folder.");

            return beatmapPath;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new InvalidOperationException(
                "Gosumemory did not respond at http://127.0.0.1:24050. Start gosumemory and try again.");
        }
        catch (HttpRequestException exception)
        {
            throw new InvalidOperationException(
                "Could not connect to gosumemory at http://127.0.0.1:24050. Start gosumemory and try again.",
                exception);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                "Gosumemory returned an invalid response. Check that its local API is available.",
                exception);
        }
    }

    private static string? GetNestedString(JsonElement value, params string[] propertyPath)
    {
        foreach (string propertyName in propertyPath)
        {
            if (value.ValueKind != JsonValueKind.Object)
                return null;

            JsonElement property = default;
            bool found = false;
            foreach (JsonProperty candidate in value.EnumerateObject())
            {
                if (!string.Equals(candidate.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                    continue;

                property = candidate.Value;
                found = true;
                break;
            }

            if (!found)
                return null;

            value = property;
        }

        return value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }
}
