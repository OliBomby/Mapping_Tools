namespace Mapping_Tools.Application.Tools.HitsoundStudio.Models;

/// <summary>Chooses how Hitsound Studio turns packages into an exported map.</summary>
public enum HitsoundStudioExportMode
{
    /// <summary>Uses osu! custom sample indices and optional greenlines.</summary>
    Standard,

    /// <summary>Places named samples at distinct positions.</summary>
    Coinciding,

    /// <summary>Writes named samples as storyboard sound events.</summary>
    Storyboard,

    /// <summary>Writes the generated SoundFont notes as a MIDI file.</summary>
    Midi,
}

