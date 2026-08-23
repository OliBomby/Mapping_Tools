using System.Text;
using System.Text.RegularExpressions;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;

namespace Mapping_Tools.Core.HitsoundStuff;

/// <summary>
///     Stores a dictionary with pairs (filename without ext., list of sample args which are satisfied by that file)
///     Represents a schema on how to exports sample packages.
/// </summary>
public class SampleSchema : Dictionary<string, List<SampleGeneratingArgs>>
{
    /// <summary>
    ///     Creates an empty export schema.
    /// </summary>
    public SampleSchema() { }

    /// <summary>
    ///     Expands custom indices into standard sample names and their required source mixes.
    /// </summary>
    /// <param name="customIndices">Index assignments to flatten into filenames.</param>
    public SampleSchema(IEnumerable<CustomIndex> customIndices)
    {
        foreach (var customIndex in customIndices)
        foreach (var customIndexSample in customIndex.Samples)
            Add(customIndexSample.Key + customIndex.GetNumberExtension(), customIndexSample.Value.ToList());
    }

    /// <summary>
    ///     Creates a schema from one-source filename assignments.
    /// </summary>
    /// <param name="sampleNames">Generation arguments mapped to extensionless export names.</param>
    public SampleSchema(Dictionary<SampleGeneratingArgs, string> sampleNames)
    {
        foreach (var sample in sampleNames)
        {
            if (string.IsNullOrEmpty(sample.Value)) continue;
            Add(sample.Value, new List<SampleGeneratingArgs> { sample.Key });
        }
    }

    /// <summary>
    ///     Make sure a certain hitsound with a certain sound is in the <see cref="SampleSchema" />.
    ///     If it already exists, then it simply returns the index and sampleset of that filename.
    /// </summary>
    /// <param name="samples">List of <see cref="SampleGeneratingArgs" /> that represents the sound that has to be made.</param>
    /// <param name="hitsoundName">Name of the hitsound. For example "hitwhistle" or "slidertick".</param>
    /// <param name="sampleSet">Sample set for the hitsound for if it adds a new sample to the sample schema.</param>
    /// <param name="newIndex">Index to start searching from. It will start at this value and go up until a slot is available.</param>
    /// <param name="newSampleSet">The sample set of the added sample.</param>
    /// <param name="startIndex">The index of the added sample.</param>
    /// <returns>True if it added a new entry.</returns>
    public bool AddHitsound(List<SampleGeneratingArgs> samples, string hitsoundName, SampleSet sampleSet, out int newIndex,
        out SampleSet newSampleSet, int startIndex = 1)
    {
        // Check if our sample schema already has a sample for this
        string? filename = FindFilename(samples, "^(normal|soft|drum)-" + hitsoundName);
        if (filename != null)
        {
            newIndex = HitsoundFilename.GetIndex(filename);
            newSampleSet = HitsoundFilename.GetSampleSet(filename);
            return false;
        }

        // Make a new sample with the same sound as all the samples mixed and add it to the sample schema
        int index = startIndex;
        newSampleSet = sampleSet;

        // Find an index which is not taken in the sample schema
        while (Keys.Any(o => Regex.IsMatch(o, "^(normal|soft|drum)-" + hitsoundName) && HitsoundFilename.GetIndex(o) == index && HitsoundFilename.GetSampleSet(o) == sampleSet))
            index++;

        newIndex = index;
        filename = $"{sampleSet.ToString().ToLower()}-{hitsoundName}{(index == 1 ? string.Empty : index.ToInvariant())}";

        Add(filename, samples);
        return true;
    }

    /// <summary>
    ///     Finds the first schema entry with the same ordered source mix.
    /// </summary>
    /// <param name="samples">The ordered generation arguments to match.</param>
    /// <returns>The export name, or <see langword="null" /> when no exact sequence exists.</returns>
    public string FindFilename(List<SampleGeneratingArgs> samples)
    {
        return (from kvp
                in this
            where kvp.Value.SequenceEqual(samples)
            select kvp.Key).FirstOrDefault();
    }

    /// <summary>
    ///     Finds the first schema entry whose name and ordered source mix both match.
    /// </summary>
    /// <param name="samples">The ordered generation arguments to match.</param>
    /// <param name="regexPattern">A regular expression applied to candidate export names.</param>
    /// <returns>The first matching name, or <see langword="null" />.</returns>
    public string FindFilename(List<SampleGeneratingArgs> samples, string regexPattern)
    {
        return (from kvp
                in this
            where kvp.Value.SequenceEqual(samples) && Regex.IsMatch(kvp.Key, regexPattern)
            select kvp.Key).FirstOrDefault();
    }

    /// <summary>
    ///     Generates a dictionary which maps <see cref="SampleGeneratingArgs" /> to their corresponding filename which makes
    ///     that sample sound.
    ///     Only maps the <see cref="SampleGeneratingArgs" /> which are non-mixed.
    /// </summary>
    /// <returns></returns>
    public Dictionary<SampleGeneratingArgs, string> GetSampleNames(SampleGeneratingArgsComparer comparer = null)
    {
        var sampleNames = new Dictionary<SampleGeneratingArgs, string>(comparer ?? new SampleGeneratingArgsComparer());

        foreach (var kvp in this.Where(kvp => kvp.Value.Count == 1))
            if (!sampleNames.ContainsKey(kvp.Value[0]))
                sampleNames.Add(kvp.Value[0], kvp.Key);

        return sampleNames;
    }

    /// <summary>
    ///     Reconstructs standard hitnormal/whistle/finish/clap custom-index requirements from schema names.
    /// </summary>
    /// <param name="comparer">The identity policy for source mixes.</param>
    /// <returns>Recognized custom indices; nonstandard names are ignored.</returns>
    public List<CustomIndex> GetCustomIndices(SampleGeneratingArgsComparer comparer = null)
    {
        if (comparer == null)
            comparer = new SampleGeneratingArgsComparer();

        var customIndices = new Dictionary<int, CustomIndex>();

        foreach (var kvp in this)
        {
            string? name = Path.GetFileNameWithoutExtension(kvp.Key);
            if (name == null) continue;

            var match = Regex.Match(name, "^(normal|soft|drum)-hit(normal|whistle|finish|clap)");
            if (!match.Success) continue;

            string hitsound = match.Value;

            string remainder = name.Substring(match.Index + match.Length);
            int index = 1;
            if (!string.IsNullOrEmpty(remainder))
                if (!FileFormatHelper.TryParseInt(remainder, out index))
                    continue;

            if (customIndices.ContainsKey(index))
            {
                customIndices[index].Samples[hitsound] = new HashSet<SampleGeneratingArgs>(kvp.Value);
            }
            else
            {
                var ci = new CustomIndex(index, comparer);
                customIndices.Add(index, ci);
                ci.Samples[hitsound] = new HashSet<SampleGeneratingArgs>(kvp.Value, comparer);
            }
        }

        return customIndices.Values.ToList();
    }

    /// <summary>
    ///     Adds missing entries from another schema and replaces only existing entries that have empty source lists.
    /// </summary>
    /// <param name="other">The lower-precedence schema to merge.</param>
    public void MergeWith(SampleSchema other)
    {
        foreach (var kvp in other)
            if (!ContainsKey(kvp.Key))
                Add(kvp.Key, kvp.Value);
            else if (this[kvp.Key].Count == 0)
                // Allow overwriting of value if the list of samples is empty, because those entries are useless
                this[kvp.Key] = kvp.Value;
    }

    /// <summary>
    ///     Formats each export name and its pipe-separated source mix for diagnostics.
    /// </summary>
    /// <returns>One schema entry per line.</returns>
    public override string ToString()
    {
        var builder = new StringBuilder();

        foreach (var kvp in this)
        {
            var sampleList = new StringBuilder();
            foreach (var sga in kvp.Value) sampleList.Append($"{sga}|");
            if (sampleList.Length > 0)
                sampleList.Remove(sampleList.Length - 1, 1);
            builder.AppendLine($"{kvp.Key}: [{sampleList}]");
        }

        return builder.ToString();
    }
}
