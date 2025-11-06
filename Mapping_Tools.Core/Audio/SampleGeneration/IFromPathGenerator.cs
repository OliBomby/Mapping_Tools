namespace Mapping_Tools.Core.Audio.SampleGeneration;

public interface IFromPathGenerator {
    /// <summary>
    /// The path to generate the sample from.
    /// </summary>
    string Path { get; }
}