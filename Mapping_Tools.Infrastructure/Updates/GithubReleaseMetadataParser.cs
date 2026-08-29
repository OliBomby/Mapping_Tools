using System.Text.Json;
using Mapping_Tools.Application.Updates.Models;

namespace Mapping_Tools.Infrastructure.Updates;

/// <summary>
///     Parses the small release-metadata payload returned by GitHub's latest-release endpoint.
/// </summary>
public static class GithubReleaseMetadataParser
{
    /// <summary>
    ///     Reads the optional release name and body without accepting arbitrary JSON as metadata.
    /// </summary>
    /// <param name="json">The UTF-8 JSON response body.</param>
    /// <returns>The title and body, or empty metadata for a JSON null response.</returns>
    /// <exception cref="JsonException">The payload is malformed or is not a JSON object.</exception>
    public static UpdateReleaseNotes Parse(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind == JsonValueKind.Null) return new UpdateReleaseNotes(null, null);

        if (document.RootElement.ValueKind != JsonValueKind.Object) throw new JsonException("The GitHub release response is not an object.");

        return new UpdateReleaseNotes(
            ReadString(document.RootElement, "name"),
            ReadString(document.RootElement, "body"));
    }

    /// <summary>
    ///     Reads the ordered release list returned by GitHub's releases endpoint.
    /// </summary>
    /// <param name="json">The UTF-8 JSON response body.</param>
    /// <returns>Release notes in the order supplied by GitHub, newest first.</returns>
    /// <exception cref="JsonException">The payload is malformed or is not a JSON array of release objects.</exception>
    public static IReadOnlyList<UpdateReleaseNotes> ParseMany(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind == JsonValueKind.Null) return [];

        if (document.RootElement.ValueKind != JsonValueKind.Array)
            throw new JsonException("The GitHub release response is not an array.");

        List<UpdateReleaseNotes> notes = [];
        foreach (var release in document.RootElement.EnumerateArray())
        {
            if (release.ValueKind != JsonValueKind.Object)
                throw new JsonException("The GitHub release response contains a non-object release.");

            string? title = ReadString(release, "name");
            if (string.IsNullOrWhiteSpace(title)) title = ReadString(release, "tag_name");
            notes.Add(new UpdateReleaseNotes(title, ReadString(release, "body")));
        }

        return notes;
    }

    private static string? ReadString(JsonElement objectElement, string propertyName)
    {
        if (!objectElement.TryGetProperty(propertyName, out var property) || property.ValueKind == JsonValueKind.Null)
            return null;

        return property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : throw new JsonException($"GitHub release property '{propertyName}' is not a string.");
    }
}
