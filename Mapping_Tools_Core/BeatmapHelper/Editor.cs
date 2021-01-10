using System.Collections.Generic;
using System.IO;
using Mapping_Tools_Core.BeatmapHelper.Parsing;

namespace Mapping_Tools_Core.BeatmapHelper {
    /// <summary>
    /// This is a class that gives it IO helper methods for an object that is parseable with a <see cref="IParser{T}"/>
    /// </summary>
    public class Editor<T> {
        protected readonly IParser<T> parser;

        /// <summary>
        /// The file path to the serialized file.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// The parsed object.
        /// </summary>
        public T Instance { get; set; }

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
        /// <param name="load">Whether to load the instance from the path</param>
        public Editor(IParser<T> parser, string path) : this(parser) {
            Path = path;
        }

        /// <inheritdoc />
        public Editor(IParser<T> parser, string path, T instance) : this(parser) {
            Path = path;
            Instance = instance;
        }

        /// <summary>
        /// Reads the file at <see cref="Path"/> and creates <see cref="Instance"/>.
        /// </summary>
        public virtual void ReadFile() {
            // Get contents of the file
            var lines = File.ReadAllLines(Path);
            Instance = parser.ParseNew(lines);
        }

        /// <summary>
        /// Saves <see cref="Instance"/> to <see cref="Path"/>.
        /// </summary>
        public virtual void SaveFile() {
            SaveFile(parser.Serialize(Instance));
        }

        /// <summary>
        /// Saves given lines to <see cref="Path"/>.
        /// </summary>
        protected virtual void SaveFile(IEnumerable<string> lines) {
            SaveFile(Path, lines);
        }

        /// <summary>
        /// Deletes existing files, creates a new one and writes all lines into the file.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="lines"></param>
        public static void SaveFile(string path, IEnumerable<string> lines) {
            if (!File.Exists(path)) {
                File.Create(path).Dispose();
            }

            File.WriteAllLines(path, lines);
        }

        /// <summary>
        /// Grabs the parent folder as absolute.
        /// </summary>
        /// <returns>The parent folder of <see cref="Path"/></returns>
        public string GetParentFolder() {
            return Directory.GetParent(Path).FullName;
        }
    }
}
