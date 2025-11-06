using System.Collections.Generic;
using Mapping_Tools.Core.Audio.SampleGeneration;
using Mapping_Tools.Core.BeatmapHelper.Enums;

namespace Mapping_Tools.Core.Audio;

/// <summary>
/// A mapping between samples and their filenames (filename without ext., set of samples which are satisfied by that file)
/// Represents a schema on how to exports sample packages.
/// </summary>
public interface ISampleSchema : IDictionary<string, ISet<ISampleGenerator>> {
    /// <summary>
    /// Makes sure a certain hitsound with a certain sound is in the <see cref="ISampleSchema"/>.
    /// If it already exists, then it simply returns the index and sampleset of that filename.
    /// </summary>
    /// <param name="samples">List of <see cref="ISampleGenerator"/> that represents the sound that has to be made.</param>
    /// <param name="hitsoundName">Name of the hitsound. For example "hitwhistle" or "slidertick".</param>
    /// <param name="sampleSet">Sample set for the hitsound for if it adds a new sample to the sample schema.</param>
    /// <param name="newIndex">Index to start searching from. It will start at this value and go up until a slot is available.</param>
    /// <param name="newSampleSet">The sample set of the added sample.</param>
    /// <param name="startIndex">The index of the added sample.</param>
    /// <returns>True if it added a new entry.</returns>
    bool AddHitsound(ISet<ISampleGenerator> samples, string hitsoundName, SampleSet sampleSet,
        out int newIndex, out SampleSet newSampleSet, int startIndex = 1);
        
    /// <summary>
    /// Finds the filename associated with the set of samples.
    /// Returns null if the set of samples could not be found in the sample schema.
    /// </summary>
    /// <param name="samples">The set of samples to search for.</param>
    /// <returns>The filename associated with the set of samples.</returns>
    string FindFilename(ISet<ISampleGenerator> samples);

    /// <summary>
    /// Finds the filename associated with the set of samples and matches the regex pattern.
    /// Returns null if the set of samples could not be found in the sample schema.
    /// </summary>
    /// <param name="samples">The set of samples to search for.</param>
    /// <param name="regexPattern">The regex pattern to match the filename with.</param>
    /// <returns>The filename associated with the set of samples.</returns>
    string FindFilename(ISet<ISampleGenerator> samples, string regexPattern);

    /// <summary>
    /// Generates a dictionary which maps <see cref="ISampleGenerator"/> to their corresponding filename which makes that sample sound.
    /// Only maps the <see cref="ISampleGenerator"/> which are non-mixed.
    /// </summary>
    /// <returns></returns>
    IDictionary<ISampleGenerator, string> GetSampleNames();

    /// <summary>
    /// Merges the other sample schema into this sample schema.
    /// </summary>
    /// <param name="other"></param>
    void MergeWith(ISampleSchema other);
}