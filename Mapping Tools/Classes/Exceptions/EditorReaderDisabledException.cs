using System;

namespace Mapping_Tools.Classes.Exceptions {
    public class EditorReaderDisabledException : Exception {
        public static readonly string EditorReaderDisabledText = "You need to enable Editor Reader to use this feature.";
        
        public EditorReaderDisabledException() : base(EditorReaderDisabledText) { }
    }
}