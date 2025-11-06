using System;

namespace Mapping_Tools.Core.Exceptions;

[Serializable]
public class BeatmapParsingException : Exception
{
    public BeatmapParsingException() : base("Unexpected value encountered while parsing beatmap.") { }

    public BeatmapParsingException(string line)
        : this(line, innerException: null) { }
    public BeatmapParsingException(string line, Exception innerException)
        : this($"Unexpected value encountered while parsing beatmap.", line, innerException) { }

    public BeatmapParsingException(string message, string line)
        : base($"{message}\n{line}", null) { }

    public BeatmapParsingException(string message, string line, Exception innerException)
        : base($"{message}\n{line}", innerException) { }
}