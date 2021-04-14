using Mapping_Tools_Core.BeatmapHelper.Decoding;
using Mapping_Tools_Core.BeatmapHelper.Encoding;
using System.IO;

namespace Mapping_Tools_Core.BeatmapHelper.Editor {
    /// <summary>
    /// A <see cref="IReadWriteEditor{T}"/> that connects an object to a file
    /// using a <see cref="IEncoder{T}"/> and a <see cref="IDecoder{T}"/>./>
    /// </summary>
    public class Editor<T> : IReadWriteEditor<T> {
        protected readonly IEncoder<T> encoder;
        protected readonly IDecoder<T> decoder;

        /// <summary>
        /// The file path to the serialized file.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Initializes a new editor.
        /// </summary>
        /// <param name="encoder">The encoder for the file type</param>
        /// <param name="decoder">The decoder for the file type</param>
        public Editor(IEncoder<T> encoder, IDecoder<T> decoder) {
            this.encoder = encoder;
            this.decoder = decoder;
        }

        /// <summary>
        /// Initializes a new editor with provided path.
        /// Optionally loads the instance from the path aswell.
        /// </summary>
        /// <param name="encoder">The encoder for the file type</param>
        /// <param name="decoder">The decoder for the file type</param>
        /// <param name="path">The path of the physical file</param>
        public Editor(IEncoder<T> encoder, IDecoder<T> decoder, string path) : this(encoder, decoder) {
            Path = path;
        }

        public virtual T ReadFile() {
            // Get contents of the file
            var lines = File.ReadAllText(Path);
            return decoder.DecodeNew(lines);
        }

        public virtual void WriteFile(T instance) {
            SaveFile(encoder.Encode(instance));
        }

        /// <summary>
        /// Saves given lines to <see cref="Path"/>.
        /// </summary>
        protected virtual void SaveFile(string lines) {
            if (!File.Exists(Path)) {
                File.Create(Path).Dispose();
            }

            File.WriteAllText(Path, lines);
        }

        public string GetParentFolder() {
            return Directory.GetParent(Path)?.FullName;
        }
    }
}
