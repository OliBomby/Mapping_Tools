namespace Mapping_Tools_Core.Audio.SampleImportArgs {
    public interface IPathSampleImportArgs : ISampleImportArgs{
        string Path { get; }

        /// <summary>
        /// Whether all the information of the sample is stored in the file
        /// described by <see cref="Path"/>.
        /// </summary>
        bool IsDirectSource();
    }
}