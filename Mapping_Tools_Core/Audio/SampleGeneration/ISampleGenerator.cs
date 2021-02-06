using System;

namespace Mapping_Tools_Core.Audio.SampleGeneration {
    public interface ISampleGenerator : IEquatable<ISampleGenerator>, ICloneable {
        /// <summary>
        /// Returns whether this sample generator is valid,
        /// so it could successfully generate a sample.
        /// </summary>
        /// <returns></returns>
        bool IsValid();

        /// <summary>
        /// Gets a string that describes this sample.
        /// Should be usable as a filename.
        /// </summary>
        /// <returns></returns>
        string GetName();
    }
}