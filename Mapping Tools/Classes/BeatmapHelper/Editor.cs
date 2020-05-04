using System.Collections.Generic;
using System.IO;

namespace Mapping_Tools.Classes.BeatmapHelper {
    /// <summary>
    /// This is a class that sits around a <see cref="ITextFile"/> and gives it IO helper methods. This makes the <see cref="ITextFile"/> more like an actual file.
    /// </summary>
    public class Editor {
        /// <summary>
        /// The file path to the beatmap or storyboard file.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// The text file interface used as an object.
        /// </summary>
        public ITextFile TextFile { get; set; }

        /// <inheritdoc />
        public Editor() {

        }

        /// <inheritdoc />
        public Editor(List<string> lines) {
            TextFile = new Beatmap(lines);
        }

        /// <inheritdoc />
        public Editor(string path) {
            Path = path;
            if (System.IO.Path.GetExtension(path) == ".osb") {
                TextFile = new StoryBoard(ReadFile(path));
            } else {
                TextFile = new Beatmap(ReadFile(path));
            }
        }

        /// <summary>
        /// Reads the text file into string formats. with the 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public List<string> ReadFile(string path) {
            // Get contents of the file
            var lines = File.ReadAllLines(path);
            return new List<string>(lines);
        }

        /// <summary>
        /// Saves the lines of string into the path provided.
        /// </summary>
        /// <param name="path"></param>
        public virtual void SaveFile(string path) {
            SaveFile(path, TextFile.GetLines());
        }

        /// <summary>
        /// Saves the lines of string into the path provided.
        /// </summary>
        /// <param name="lines"></param>
        public virtual void SaveFile(List<string> lines) {
            SaveFile(Path, lines);
        }

        /// <summary>
        /// Saves the beatmap files.
        /// </summary>
        public virtual void SaveFile() {
            SaveFile(Path, TextFile.GetLines());
        }

        /// <summary>
        /// Deletes existing files, creates a new one and writes all lines into the file.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="lines"></param>
        public static void SaveFile(string path, List<string> lines) {
            if (!File.Exists(path)) {
                File.Create(path).Dispose();
            }

            File.WriteAllLines(path, lines);
        }

        /// <summary>
        /// Grab the parent folder as absolute.
        /// </summary>
        /// <returns></returns>
        public string GetParentFolder() {
            return Directory.GetParent(Path).FullName;
        }

        /// <summary>
        /// Grab the parent folder as absolute.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetParentFolder(string path)
        {
            return Directory.GetParent(path).FullName;
        }
    }
}
