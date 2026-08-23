namespace Mapping_Tools.Core.Classes.HitsoundStuff;

/// <summary>
///     Enumerator of import types of hitsounds.
/// </summary>
public enum ImportType
{
    /// <summary>
    ///     No source has been selected.
    /// </summary>
    None = 0,

    /// <summary>
    ///     Imports samples by matching stacked hit objects.
    /// </summary>
    Stack = 1,

    /// <summary>
    ///     Imports samples from beatmap hitsound assignments.
    /// </summary>
    Hitsounds = 2,

    // ReSharper disable once InconsistentNaming
    /// <summary>
    ///     Imports notes and timing from MIDI data.
    /// </summary>
    MIDI = 3,

    /// <summary>
    ///     Imports samples referenced by storyboard sound events.
    /// </summary>
    Storyboard = 4,
}
