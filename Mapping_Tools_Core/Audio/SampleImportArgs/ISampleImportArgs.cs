using System;
using System.Collections.Generic;
using Mapping_Tools_Core.Audio.SampleSoundGeneration;

namespace Mapping_Tools_Core.Audio.SampleImportArgs {
    public interface ISampleImportArgs : IEquatable<ISampleImportArgs>, ICloneable {
        bool IsValid();
        bool IsValid(Dictionary<ISampleImportArgs, ISampleSoundGenerator> loadedSamples);
        ISampleSoundGenerator Import();
    }
}