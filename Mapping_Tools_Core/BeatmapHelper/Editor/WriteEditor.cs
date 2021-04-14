using Mapping_Tools_Core.BeatmapHelper.Encoding;
using System.IO;

namespace Mapping_Tools_Core.BeatmapHelper.Editor {
    /// <summary>
    /// A <see cref="IWriteEditor{T}"/> that helps write an object to a file using a <see cref="IEncoder{T}"/>./>
    /// </summary>
    public class WriteEditor<T> : IWriteEditor<T> {
        protected readonly IEncoder<T> encoder;

        /// <summary>
        /// The file path to the serialized file.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Initializes a new editor.
        /// </summary>
        /// <param name="encoder">The encoder for the file type</param>
        public WriteEditor(IEncoder<T> encoder) {
            this.encoder = encoder;
        }

        /// <summary>
        /// Initializes a new editor with provided path.
        /// Optionally loads the instance from the path aswell.
        /// </summary>
        /// <param name="encoder">The encoder for the file type</param>
        /// <param name="path">The path of the physical file</param>
        public WriteEditor(IEncoder<T> encoder, string path) : this(encoder) {
            Path = path;
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
