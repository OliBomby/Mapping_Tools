namespace Mapping_Tools.Application.Settings.Models;

/// <summary>Chooses how Mapping Tools reads unsaved beatmap editor state.</summary>
public enum BeatmapLiveStateReadingMode
{
    /// <summary>Do not read unsaved editor state.</summary>
    Disabled,

    /// <summary>Read unsaved editor state through the Editor Reader adapter.</summary>
    EditorReader,

    /// <summary>Read unsaved editor state through MTIPC.</summary>
    Mtipc,
}
