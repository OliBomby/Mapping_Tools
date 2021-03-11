namespace Mapping_Tools_Core.Audio.SampleGeneration {
    public interface IFromPathGenerator {
        /// <summary>
        /// The path to generate the sample from.
        /// </summary>
        string Path { get; }
    }
}