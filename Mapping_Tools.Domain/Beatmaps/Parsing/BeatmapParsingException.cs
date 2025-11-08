namespace Mapping_Tools.Domain.Beatmaps.Parsing;

[Serializable]
public class BeatmapParsingException : Exception
{
    public BeatmapParsingException() : base("Unexpected value encountered while parsing beatmap.") { }

    public BeatmapParsingException(string line, Exception? innerException = null)
        : this("Unexpected value encountered while parsing beatmap.", line, innerException) { }

    public BeatmapParsingException(string message, string line)
        : base($"{message}\n{line}", null) { }

    public BeatmapParsingException(string message, string line, Exception? innerException = null)
        : base($"{message}\n{line}", innerException) { }
}