using System;
using Mapping_Tools_Core.Audio.SampleSoundGeneration;

namespace Mapping_Tools_Core.Audio.SampleImportArgs {
    public interface ISampleImportArgs : IEquatable<ISampleImportArgs> {
        bool IsValid();
        ISampleSoundGenerator Import();
    }
}