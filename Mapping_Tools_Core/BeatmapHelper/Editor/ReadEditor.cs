using Mapping_Tools_Core.BeatmapHelper.Decoding;
using System.IO;

namespace Mapping_Tools_Core.BeatmapHelper.Editor {
    /// <summary>
    /// A <see cref="IReadEditor{T}"/> that connects an object to a file
    /// using a <see cref="IDecoder{T}"/>./>
    /// </summary>
    public class ReadEditor<T> : IReadEditor<T> {
        protected readonly IDecoder<T> decoder;

        /// <summary>
        /// The file path to the serialized file.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Initializes a new editor.
        /// </summary>
        /// <param name="decoder">The decoder for the file type</param>
        public ReadEditor(IDecoder<T> decoder) {
            this.decoder = decoder;
        }

        /// <summary>
        /// Initializes a new editor with provided path.
        /// Optionally loads the instance from the path aswell.
        /// </summary>
        /// <param name="decoder">The decoder for the file type</param>
        /// <param name="path">The path of the physical file</param>
        public ReadEditor(IDecoder<T> decoder, string path) : this(decoder) {
            Path = path;
        }

        public virtual T ReadFile() {
            // Get contents of the file
            var lines = File.ReadAllText(Path);
            return decoder.DecodeNew(lines);
        }

        public string GetParentFolder() {
            return Directory.GetParent(Path)?.FullName;
        }
    }
}
