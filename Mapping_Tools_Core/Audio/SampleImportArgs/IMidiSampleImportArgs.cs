namespace Mapping_Tools_Core.Audio.SampleImportArgs {
    /// <summary>
    /// Represents the arguments for a single MIDI note.
    /// </summary>
    public interface IMidiSampleImportArgs : ISampleImportArgs {
        int Bank { get; }
        int Patch { get; }
        int Instrument { get; }
        int Key { get; }
        int Velocity { get; }
        double Length { get; }
    }
}