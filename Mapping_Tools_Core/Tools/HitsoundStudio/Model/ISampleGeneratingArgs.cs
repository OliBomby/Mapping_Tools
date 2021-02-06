using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Mapping_Tools_Core.Audio.SampleGeneration;
using Mapping_Tools_Core.Audio.SampleSoundGeneration;

namespace Mapping_Tools_Core.Tools.HitsoundStudio.Model {
    /// <summary>
    /// Arguments for both importing and post-processing of audio samples.
    /// </summary>
    public interface ISampleGeneratingArgs : IEquatable<ISampleGeneratingArgs>, ICloneable {
        /// <summary>
        /// How to import/generate the sample.
        /// If null, no sample will be generated.
        /// </summary>
        [CanBeNull]
        ISampleGenerator ImportArgs { get; }

        /// <summary>
        /// Volume post-processing argument.
        /// </summary>
        double Volume { get; }

        bool IsValid();

        bool IsValid(Dictionary<ISampleGeneratingArgs, ISampleSoundGenerator> loadedSamples);

        /// <summary>
        /// Whether any post-processing will be applied to the sample
        /// which will change the sample.
        /// </summary>
        /// <returns></returns>
        bool HasEffects();

        /// <summary>
        /// Factory method for the <see cref="ISampleSoundGenerator"/>.
        /// Uses the <see cref="ImportArgs"/> and applies the additional effects.
        /// Returns null if import failed.
        /// </summary>
        /// <returns></returns>
        ISampleSoundGenerator Import();

        /// <summary>
        /// Applies the additional effects to given generator.
        /// </summary>
        /// <param name="baseGenerator"></param>
        /// <returns></returns>
        ISampleSoundGenerator ApplyEffects(ISampleSoundGenerator baseGenerator);

        /// <summary>
        /// Gets a string that describes this sample.
        /// Should be usable as a filename.
        /// </summary>
        /// <returns></returns>
        string GetName();
    }
}