namespace Mapping_Tools.Core.BeatmapHelper.IO.Editor;

/// <summary>
/// Read interface./>
/// </summary>
public interface IReadEditor<out T> : IEditor {
    /// <summary>
    /// Reads and parses the object.
    /// </summary>
    /// <returns>The parsed object.</returns>
    T ReadFile();
}