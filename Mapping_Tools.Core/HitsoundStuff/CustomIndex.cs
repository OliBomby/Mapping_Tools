using System.Text;
using Mapping_Tools.Core.BeatmapHelper;

namespace Mapping_Tools.Core.HitsoundStuff;

/// <summary>
///     Describes the source mixes assigned to all twelve standard hitsound slots for one custom sample index.
/// </summary>
public class CustomIndex
{
    /// <summary>
    ///     The complete normal/soft/drum by normal/whistle/finish/clap key space.
    /// </summary>
    public static readonly List<string> AllKeys = new()
    {
        "normal-hitnormal", "normal-hitwhistle", "normal-hitfinish", "normal-hitclap",
        "soft-hitnormal", "soft-hitwhistle", "soft-hitfinish", "soft-hitclap",
        "drum-hitnormal", "drum-hitwhistle", "drum-hitfinish", "drum-hitclap",
    };

    private readonly SampleGeneratingArgsComparer comparer;

    /// <summary>
    ///     The osu! custom index, or -1 while the requirement is unassigned.
    /// </summary>
    public int Index;

    /// <summary>
    ///     Source mixes keyed by names such as <c>normal-hitclap</c>.
    /// </summary>
    public Dictionary<string, HashSet<SampleGeneratingArgs>> Samples;

    /// <summary>
    ///     Creates an assigned, initially empty custom-index requirement.
    /// </summary>
    /// <param name="index">The assigned custom sample index.</param>
    /// <param name="comparer">The source identity policy, or null to use the default policy.</param>
    public CustomIndex(int index, SampleGeneratingArgsComparer? comparer = null)
    {
        Index = index;
        this.comparer = comparer ?? new SampleGeneratingArgsComparer();
        Samples = new Dictionary<string, HashSet<SampleGeneratingArgs>>();
        foreach (string key in AllKeys) Samples[key] = new HashSet<SampleGeneratingArgs>(this.comparer);
    }

    /// <summary>
    ///     Creates an unassigned, initially empty custom-index requirement.
    /// </summary>
    /// <param name="comparer">The source identity policy, or null to use the default policy.</param>
    public CustomIndex(SampleGeneratingArgsComparer? comparer = null)
    {
        Index = -1;
        this.comparer = comparer ?? new SampleGeneratingArgsComparer();
        Samples = new Dictionary<string, HashSet<SampleGeneratingArgs>>();
        foreach (string key in AllKeys) Samples[key] = new HashSet<SampleGeneratingArgs>(this.comparer);
    }

    /// <summary>
    ///     Tests whether an existing slot supports a requested slot exactly or the request is empty.
    /// </summary>
    /// <param name="s1">The existing source mix.</param>
    /// <param name="s2">The requested source mix.</param>
    /// <returns>Whether the requested mix is empty or identical to the existing mix.</returns>
    public static bool CheckSupport(HashSet<SampleGeneratingArgs> s1, HashSet<SampleGeneratingArgs> s2)
    {
        // s2 fits in s1 or s2 is empty
        return s2.Count <= 0 || s1.SetEquals(s2);
    }

    /// <summary>
    ///     Tests whether two slots can merge because at least one is empty or both source sets match.
    /// </summary>
    /// <param name="s1">The first source mix.</param>
    /// <param name="s2">The second source mix.</param>
    /// <returns>Whether either mix is empty or both mixes are identical.</returns>
    public static bool CheckCanSupport(HashSet<SampleGeneratingArgs> s1, HashSet<SampleGeneratingArgs> s2)
    {
        // s2 fits in s1 or s1 is empty or s2 is empty
        return s1.Count <= 0 || s2.Count <= 0 || s1.SetEquals(s2);
    }

    /// <summary>
    ///     Determines whether every non-empty requirement in another index is already supported by this index.
    /// </summary>
    /// <param name="other">The requested custom-index requirements.</param>
    /// <returns>Whether this index satisfies every requested slot.</returns>
    public bool Fits(CustomIndex other)
    {
        // Every non-empty set from other == set from self
        // True until false
        bool support = true;
        foreach (var kvp in Samples) support = CheckSupport(kvp.Value, other.Samples[kvp.Key]) && support;
        return support;
    }

    /// <summary>
    ///     Determines whether two custom-index requirements can combine without conflicting source mixes.
    /// </summary>
    /// <param name="other">The custom index to combine with this index.</param>
    /// <returns>Whether combining the indices introduces no conflicting source mix.</returns>
    public bool CanMerge(CustomIndex other)
    {
        // Every non-empty set from other == non-empty set from self
        // True until false
        bool support = true;
        foreach (var kvp in Samples) support = CheckCanSupport(kvp.Value, other.Samples[kvp.Key]) && support;
        return support;
    }

    /// <summary>
    ///     Unions another compatible requirement into this instance and adopts its assigned index when needed.
    /// </summary>
    /// <param name="other">The compatible custom index whose source mixes are added.</param>
    public void MergeWith(CustomIndex other)
    {
        foreach (string key in AllKeys) Samples[key].UnionWith(other.Samples[key]);

        // If the other custom index has an assigned index and this one doesnt. Get the index, so optimised custom indices retain their indices.
        if (Index == -1 && other.Index != -1) Index = other.Index;
    }

    /// <summary>
    ///     Creates a combined requirement using the larger assigned index.
    /// </summary>
    /// <param name="other">The custom index to combine with this index.</param>
    /// <returns>A new custom index containing the combined source requirements.</returns>
    public CustomIndex Merge(CustomIndex other)
    {
        var ci = new CustomIndex(Math.Max(Index, other.Index));
        foreach (string key in AllKeys) ci.Samples[key].UnionWith(other.Samples[key]);
        return ci;
    }

    /// <summary>
    ///     Copies every slot with the same sample-argument comparer.
    /// </summary>
    /// <returns>An independent copy of the slot sets using the same identity policy.</returns>
    public CustomIndex Copy()
    {
        var ci = new CustomIndex(Index, comparer);
        ci.MergeWith(this);
        return ci;
    }

    /// <summary>
    ///     Removes invalid sources and leaves an empty-argument sentinel when an occupied slot loses every source.
    /// </summary>
    /// <param name="isValid">Validation policy supplied by the caller.</param>
    public void CleanInvalids(Func<SampleGeneratingArgs, bool> isValid)
    {
        if (isValid is null) throw new ArgumentNullException(nameof(isValid));

        // Replace all invalid paths with "" and remove the invalid path if another valid path is also in the hashset
        foreach (var paths in Samples.Values)
        {
            int initialCount = paths.Count;
            paths.RemoveWhere(o => !isValid(o));

            if (paths.Count == 0 && initialCount != 0)
                // All the paths where invalid and it didn't just start out empty
                paths.Add(new SampleGeneratingArgs()); // This "" is here to prevent this hashset from getting new paths
        }
    }

    /// <summary>
    ///     Formats every standard slot and its source mix for diagnostics.
    /// </summary>
    /// <returns>Each slot name followed by its source mix.</returns>
    public override string ToString()
    {
        var accumulator = new StringBuilder();
        foreach (var kvp in Samples)
        {
            var sampleList = new StringBuilder();
            foreach (var sga in kvp.Value) sampleList.Append($"{sga}|");
            if (sampleList.Length > 0)
                sampleList.Remove(sampleList.Length - 1, 1);
            accumulator.Append($"{kvp.Key}: [{sampleList}]");
        }

        return accumulator.ToString();
    }

    /// <summary>
    ///     Formats the custom-index suffix used in standard osu! sample filenames.
    /// </summary>
    /// <returns>An empty suffix for index one; otherwise the invariant decimal index.</returns>
    public string GetNumberExtension()
    {
        return Index == 1 ? string.Empty : Index.ToInvariant();
    }
}
