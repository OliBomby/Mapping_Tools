namespace Mapping_Tools_Core.Audio.SampleGeneration.Decorators {
    public interface ISampleDecorator : ISampleGenerator {
        /// <summary>
        /// Whether this decorator changes anything about the sample.
        /// </summary>
        /// <returns></returns>
        bool HasEffect();

        ISampleGenerator BaseGenerator { get; }
    }
}