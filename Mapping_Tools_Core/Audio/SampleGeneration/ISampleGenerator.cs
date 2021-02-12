using System;
using Mapping_Tools_Core.Audio.Exporting;

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

        /// <summary>
        /// Writes the data of this sample to the exporter.
        /// </summary>
        /// <param name="exporter">The exporter to write the sample to.</param>
        void ToExporter(ISampleExporter exporter);
    }
}