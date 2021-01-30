using System;
using System.Collections.Generic;
using Mapping_Tools_Core.Audio.SampleSoundGeneration;

namespace Mapping_Tools_Core.Audio.SampleImportArgs {
    public interface ISampleImportArgs : IEquatable<ISampleImportArgs>, ICloneable {
        bool IsValid();

        bool IsValid(Dictionary<ISampleImportArgs, ISampleSoundGenerator> loadedSamples);

        /// <summary>
        /// Imports the sample using these arguments.
        /// Returns null if import failed.
        /// </summary>
        /// <returns></returns>
        ISampleSoundGenerator Import();

        /// <summary>
        /// Gets a string that describes this sample.
        /// Should be usable as a filename.
        /// </summary>
        /// <returns></returns>
        string GetName();
    }
}