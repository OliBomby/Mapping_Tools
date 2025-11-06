namespace Mapping_Tools.Core.Audio.SampleGeneration;

public interface IPreloadableGenerator {
    /// <summary>
    /// Do the necessary pre-loading and caching to make sample generation fast.
    /// </summary>
    void PreloadSample();
}