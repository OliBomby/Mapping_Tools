using System;

namespace Mapping_Tools_Core.Exceptions {
    public class InvalidEditorReaderStateException : Exception {
        public static readonly string InvalidEditorReaderStateText = "Failed to validate Editor Reader state.";

        public InvalidEditorReaderStateException() : base(InvalidEditorReaderStateText) { }
    }
}