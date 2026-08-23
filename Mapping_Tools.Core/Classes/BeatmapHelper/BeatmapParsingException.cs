namespace Mapping_Tools.Core.Classes.BeatmapHelper;

/// <summary>
///     Reports malformed osu! beatmap text while retaining the offending source line in the message.
/// </summary>
[Serializable]
#nullable disable
public class BeatmapParsingException : Exception
{
    /// <summary>
    ///     Creates an exception without parser context.
    /// </summary>
    public BeatmapParsingException()
    {
    }

    /// <summary>
    ///     Creates an exception for an unexpected value on a source line.
    /// </summary>
    /// <param name="line">The complete beatmap line that could not be parsed.</param>
    public BeatmapParsingException(string line)
        : base($"Unexpected value encountered while parsing beatmap.\n{line}")
    {
    }

    /// <summary>
    ///     Creates an exception with a specific parser diagnostic and the offending source line.
    /// </summary>
    /// <param name="message">A description of the invalid field or format.</param>
    /// <param name="line">The complete beatmap line that could not be parsed.</param>
    public BeatmapParsingException(string message, string line)
        : base($"{message}\n{line}")
    {
    }
}
