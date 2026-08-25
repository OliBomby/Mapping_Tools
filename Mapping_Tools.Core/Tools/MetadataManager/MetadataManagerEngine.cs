using Mapping_Tools.Core.BeatmapHelper;

namespace Mapping_Tools.Core.Tools.MetadataManager;

/// <summary>
///     Applies and extracts Metadata Manager state without filesystem or frontend dependencies.
/// </summary>
public static class MetadataManagerEngine
{
    /// <summary>
    ///     Reads the metadata fields and colour sections from a parsed beatmap.
    /// </summary>
    /// <param name="beatmap">The beatmap to inspect.</param>
    /// <returns>A new options object containing independent editable colour instances.</returns>
    public static MetadataManagerEngineOptions Read(Beatmap beatmap)
    {
        ArgumentNullException.ThrowIfNull(beatmap);

        MetadataManagerEngineOptions options = new()
        {
            Artist = GetText(beatmap.Metadata, "ArtistUnicode"),
            RomanisedArtist = GetText(beatmap.Metadata, "Artist"),
            Title = GetText(beatmap.Metadata, "TitleUnicode"),
            RomanisedTitle = GetText(beatmap.Metadata, "Title"),
            BeatmapCreator = GetText(beatmap.Metadata, "Creator"),
            Source = GetText(beatmap.Metadata, "Source"),
            Tags = GetText(beatmap.Metadata, "Tags"),
            PreviewTime = beatmap.General.TryGetValue("PreviewTime", out var preview)
                ? preview.DoubleValue
                : -1,
            ComboColours = beatmap.ComboColours
                .Select(colour => new ComboColour(colour.Color))
                .ToList(),
            SpecialColours = beatmap.SpecialColours
                .Select(pair => new SpecialColour(pair.Value.Color, pair.Key))
                .ToList(),
        };

        return options;
    }

    /// <summary>
    ///     Applies all configured metadata fields to a parsed beatmap while leaving
    ///     timing, events, difficulty settings, and hit objects untouched.
    /// </summary>
    /// <param name="beatmap">The mutable target beatmap.</param>
    /// <param name="options">The values to write.</param>
    public static void Apply(Beatmap beatmap, MetadataManagerEngineOptions options)
    {
        ArgumentNullException.ThrowIfNull(beatmap);
        ArgumentNullException.ThrowIfNull(options);

        beatmap.Metadata["ArtistUnicode"] = new StringValue(options.Artist);
        beatmap.Metadata["Artist"] = new StringValue(options.RomanisedArtist);
        beatmap.Metadata["TitleUnicode"] = new StringValue(options.Title);
        beatmap.Metadata["Title"] = new StringValue(options.RomanisedTitle);
        beatmap.Metadata["Creator"] = new StringValue(options.BeatmapCreator);
        beatmap.Metadata["Source"] = new StringValue(options.Source);
        beatmap.Metadata["Tags"] = new StringValue(
            options.DoRemoveDuplicateTags
                ? NormalizeTags(options.Tags)
                : options.Tags);

        beatmap.General["PreviewTime"] = new StringValue(
            Math.Round(options.PreviewTime).ToInvariant());

        if (options.UseComboColours)
        {
            beatmap.ComboColours = options.ComboColours
                .Select(colour => new ComboColour(colour.Color))
                .ToList();
            beatmap.SpecialColours.Clear();
            foreach (var specialColour in options.SpecialColours)
            {
                if (string.IsNullOrWhiteSpace(specialColour.Name))
                    throw new ArgumentException(
                        "Every special colour must have a name.",
                        nameof(options));

                beatmap.SpecialColours.Add(
                    specialColour.Name,
                    new ComboColour(specialColour.Color));
            }
        }

        if (options.ResetIds)
        {
            beatmap.Metadata["BeatmapID"] = new StringValue("0");
            beatmap.Metadata["BeatmapSetID"] = new StringValue("-1");
        }
    }

    /// <summary>
    ///     Removes repeated space-delimited tags while retaining their first-seen order.
    /// </summary>
    /// <param name="tags">The original space-delimited tag text.</param>
    /// <returns>The normalized tag text.</returns>
    public static string NormalizeTags(string tags)
    {
        ArgumentNullException.ThrowIfNull(tags);
        return string.Join(
            ' ',
            new HashSet<string>(
                tags.Split(' '),
                StringComparer.Ordinal));
    }

    private static string GetText(
        IReadOnlyDictionary<string, StringValue> values,
        string key)
    {
        return values.TryGetValue(key, out var value)
            ? value.Value ?? string.Empty
            : string.Empty;
    }
}
