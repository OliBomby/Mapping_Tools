namespace Mapping_Tools.Core.Audio;

/// <summary>Owns the note and volume events of an imported or generated MIDI sequence.</summary>
public sealed class MidiSequence
{
    /// <summary>Creates a sequence and copies its event collections.</summary>
    /// <param name="notes">The note events.</param>
    /// <param name="volumeChanges">Optional main-volume events.</param>
    public MidiSequence(IEnumerable<MidiNote> notes, IEnumerable<MidiVolumeChange>? volumeChanges = null)
    {
        ArgumentNullException.ThrowIfNull(notes);
        Notes = Array.AsReadOnly(notes.ToArray());
        VolumeChanges = Array.AsReadOnly((volumeChanges ?? []).ToArray());
    }

    /// <summary>Gets the imported note events.</summary>
    public IReadOnlyList<MidiNote> Notes { get; }

    /// <summary>Gets the imported main-volume events.</summary>
    public IReadOnlyList<MidiVolumeChange> VolumeChanges { get; }
}
