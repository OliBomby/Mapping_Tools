using Mapping_Tools.Core.BeatmapHelper.Events;
using static Mapping_Tools.Core.BeatmapHelper.FileFormatHelper;

namespace Mapping_Tools.Core.BeatmapHelper.Serialization;

/// <summary>Encodes beatmap models as complete osu! beatmap documents.</summary>
public sealed class BeatmapEncoder : IBeatmapEncoder
{
    private readonly IStoryboardEncoder storyboardEncoder;

    /// <summary>Creates an encoder with the default storyboard encoder.</summary>
    public BeatmapEncoder() : this(new StoryboardEncoder()) { }

    /// <summary>Creates an encoder that uses the supplied storyboard encoder.</summary>
    /// <param name="storyboardEncoder">The encoder for the embedded events section.</param>
    public BeatmapEncoder(IStoryboardEncoder storyboardEncoder)
    {
        this.storyboardEncoder = storyboardEncoder ?? throw new ArgumentNullException(nameof(storyboardEncoder));
    }

    /// <inheritdoc />
    public string Encode(Beatmap beatmap)
    {
        ArgumentNullException.ThrowIfNull(beatmap);
        int version = beatmap.Version;

        // Build the document sections in osu!'s canonical order.
        var lines = new List<string>
        {
            "osu file format v" + version.ToInvariant(),
            "",
            "[General]",
        };
        AddDictionaryToLines(beatmap.General, lines, true);
        lines.Add("");
        lines.Add("[Editor]");
        AddDictionaryToLines(beatmap.Editor, lines, true);
        lines.Add("");
        lines.Add("[Metadata]");
        AddDictionaryToLines(beatmap.Metadata, lines, version >= 128);
        lines.Add("");
        lines.Add("[Difficulty]");
        AddDictionaryToLines(beatmap.Difficulty, lines, version >= 128);
        lines.Add("");

        // Let the storyboard codec format events for the beatmap's source version.
        string storyboardText = storyboardEncoder.Encode(beatmap.StoryBoard, version);
        var eventLines = storyboardText.Split(["\r\n", "\n"], StringSplitOptions.None).ToList();

        // Remove the standalone storyboard's final empty line before composing the next section.
        while (eventLines.Count > 0 && eventLines[^1].Length == 0)
            eventLines.RemoveAt(eventLines.Count - 1);
        int backgroundLayerHeader = eventLines.IndexOf("//Storyboard Layer 0 (Background)");
        if (backgroundLayerHeader >= 0)
        {
            // Breaks sit before storyboard layer events in the file; old formats label that group.
            if (version < 128)
                eventLines.Insert(backgroundLayerHeader++, "//Break Periods");

            eventLines.InsertRange(
                backgroundLayerHeader,
                beatmap.BreakPeriods.Select(value => EventTextCodec.EncodeLine(value, version)));
        }

        // Lazer does not write category comments; older versions expect the overlay header.
        if (version >= 128)
        {
            eventLines.RemoveAll(line => line.StartsWith("//"));
        }
        else if (!eventLines.Contains("//Storyboard Layer 4 (Overlay)"))
        {
            int sampleHeader = eventLines.IndexOf("//Storyboard Sound Samples");
            if (sampleHeader >= 0)
                eventLines.Insert(sampleHeader, "//Storyboard Layer 4 (Overlay)");
        }
        lines.AddRange(eventLines);
        lines.Add("");
        lines.Add("[TimingPoints]");
        lines.AddRange(
            beatmap.BeatmapTiming.TimingPoints.Select(point => TimingPointTextCodec.Encode(point, version)));

        // The legacy file layout has a blank line before an optional colours section.
        if (version < 128) lines.Add("");

        if (beatmap.ComboColours.Any())
        {
            lines.Add("");
            lines.Add("[Colours]");
            lines.AddRange(beatmap.ComboColours.Select(
                (colour, index) => "Combo" + (index + 1) + (version < 128 ? " : " : ": ") + colour));
            lines.AddRange(beatmap.SpecialColours.Select(
                colour => colour.Key + (version < 128 ? " : " : ": ") + colour.Value));
        }

        lines.Add("");
        lines.Add("[HitObjects]");
        lines.AddRange(
            beatmap.HitObjects.Select(hitObject => HitObjectTextCodec.Encode(hitObject, version)));
        lines.Add("");
        return string.Join("\r\n", lines) + "\r\n";
    }
}
