using System.Collections.Generic;
using System.IO;
using Mapping_Tools_Core.BeatmapHelper.Parsing;

namespace Mapping_Tools_Core.BeatmapHelper.Editor {
    /// <summary>
    /// This is a class that gives it IO helper methods for an object that is parseable with a <see cref="IParser{T}"/>
    /// </summary>
    public class Editor<T> : IReadWriteEditor<T> {
        protected readonly IParser<T> parser;

        /// <summary>
        /// The file path to the serialized file.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Initializes a new editor.
        /// </summary>
        /// <param name="parser">The parser for the file type</param>
        public Editor(IParser<T> parser) {
            this.parser = parser;
        }

        /// <summary>
        /// Initializes a new editor with provided path.
        /// Optionally loads the instance from the path aswell.
        /// </summary>
        /// <param name="parser">The parser for the file type</param>
        /// <param name="path">The path of the physical file</param>
        public Editor(IParser<T> parser, string path) : this(parser) {
            Path = path;
        }

        public virtual T ReadFile() {
            // Get contents of the file
            var lines = File.ReadAllLines(Path);
            return parser.ParseNew(lines);
        }

        public virtual void WriteFile(T instance) {
            SaveFile(parser.Serialize(instance));
        }

        /// <summary>
        /// Saves given lines to <see cref="Path"/>.
        /// </summary>
        protected virtual void SaveFile(IEnumerable<string> lines) {
            if (!File.Exists(Path)) {
                File.Create(Path).Dispose();
            }

            File.WriteAllLines(Path, lines);
        }

        public string GetParentFolder() {
            return Directory.GetParent(Path).FullName;
        }
    }
}
