using System;

namespace Mapping_Tools_Core.Exceptions
{
    [Serializable]
    class BeatmapParsingException : Exception
    {
        public BeatmapParsingException() {

        }

        public BeatmapParsingException(string line)
            : base($"Unexpected value encountered while parsing beatmap.\n{line}") {

        }

        public BeatmapParsingException(string message, string line)
            : base($"{message}\n{line}") {

        }
    }
}
