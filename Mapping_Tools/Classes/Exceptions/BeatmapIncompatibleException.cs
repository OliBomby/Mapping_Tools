using System;

namespace Mapping_Tools.Classes.Exceptions {
    public class BeatmapIncompatibleException : Exception {
        public static readonly string BeatmapIncompatibleText = "This beatmap is incompatible with this operation.";
        
        public BeatmapIncompatibleException() : base(BeatmapIncompatibleText) { }

        public BeatmapIncompatibleException(string message) : base(message) { }

        public BeatmapIncompatibleException(string message, Exception innerException) : base(message, innerException) { }
    }
}