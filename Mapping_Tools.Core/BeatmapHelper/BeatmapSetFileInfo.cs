using System.IO;

namespace Mapping_Tools.Core.BeatmapHelper;

public class BeatmapSetFileInfo : IBeatmapSetFileInfo {
    private readonly string rootPath;

    public string Filename { get; }
    public long Size => new FileInfo(GetFullPath()).Length;

    public BeatmapSetFileInfo(string rootPath, string filename) {
        this.rootPath = rootPath;
        Filename = filename;
    }

    private string GetFullPath() {
        return Path.Join(rootPath, Filename);
    }

    public Stream GetData() {
        return File.OpenRead(GetFullPath());
    }

    public bool Equals(IBeatmapSetFileInfo other) {
        return other != null && Filename == other.Filename;
    }

    public override bool Equals(object obj) {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((IBeatmapSetFileInfo) obj);
    }

    public override int GetHashCode() {
        return Filename != null ? Filename.GetHashCode() : 0;
    }
}