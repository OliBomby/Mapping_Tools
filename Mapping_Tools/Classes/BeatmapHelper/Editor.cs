using System;
using System.Collections.Generic;
using Mapping_Tools.ApplicationServices.Abstractions;
using Mapping_Tools.Infrastructure.Files;

namespace Mapping_Tools.Classes.BeatmapHelper {
    /// <summary>
    /// Legacy compatibility wrapper around an <see cref="ITextFile"/> and its persistence adapter.
    /// </summary>
    public class Editor {
        private static readonly ITextFileStore DefaultFileStore = new FileSystemTextFileStore();

        protected ITextFileStore FileStore { get; }

        public string Path { get; set; }

        public ITextFile TextFile { get; set; }

        public Editor() : this(DefaultFileStore) { }

        public Editor(ITextFileStore fileStore) {
            FileStore = fileStore ?? throw new ArgumentNullException(nameof(fileStore));
        }

        public Editor(List<string> lines) : this(lines, DefaultFileStore) { }

        public Editor(List<string> lines, ITextFileStore fileStore) : this(fileStore) {
            TextFile = new Beatmap(lines);
        }

        public Editor(string path) : this(path, DefaultFileStore) { }

        public Editor(string path, ITextFileStore fileStore) : this(fileStore) {
            Path = path;
            TextFile = System.IO.Path.GetExtension(path).ToLowerInvariant() == ".osb"
                ? new StoryBoard(ReadFile(path))
                : new Beatmap(ReadFile(path));
        }

        public List<string> ReadFile(string path) => new(FileStore.ReadAllLines(path));

        public virtual void SaveFile(string path) {
            FileStore.WriteAllLines(path, TextFile.GetLines());
        }

        public virtual void SaveFile(List<string> lines) {
            FileStore.WriteAllLines(Path, lines);
        }

        public virtual void SaveFile() {
            FileStore.WriteAllLines(Path, TextFile.GetLines());
        }

        public static void SaveFile(string path, List<string> lines) {
            DefaultFileStore.WriteAllLines(path, lines);
        }

        public string GetParentFolder() => FileStore.GetParentFolder(Path);

        public static string GetParentFolder(string path) => DefaultFileStore.GetParentFolder(path);
    }
}
