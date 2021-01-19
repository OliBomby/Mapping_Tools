namespace Mapping_Tools_Core.BeatmapHelper.Editor {
    public interface IReadEditor<out T> {
        string Path { get; set; }
        T Instance { get; }
        //TODO: Contracts so that Path and Instance always refer to the same object. Or just a method for Read(string path)
    }
}