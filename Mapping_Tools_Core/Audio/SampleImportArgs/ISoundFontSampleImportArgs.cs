namespace Mapping_Tools_Core.Audio.SampleImportArgs {
    public interface ISoundFontSampleImportArgs : IPathSampleImportArgs {
        int Bank { get; }
        int Patch { get; }
        int Instrument { get; }
        int Key { get; }
        int Velocity { get; }
        double Length { get; }
    }
}