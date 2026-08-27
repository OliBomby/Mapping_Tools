using Mapping_Tools.Core.Audio;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.HitsoundStuff;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.HitsoundStudio.Models;

namespace Mapping_Tools.Core.Tools.HitsoundStudio;

/// <summary>
///     Applies the framework-neutral layer, package, custom-index, and schema
///     algorithms used by Hitsound Studio.
/// </summary>
/// <remarks>
///     File decoding, SoundFont rendering, playback, and encoding deliberately do
///     not appear here. Callers provide a source-validation policy so the same
///     rules work for ordinary audio exports, MIDI-chord exports, and tests.
/// </remarks>
public sealed class HitsoundStudioEngine
{
    /// <summary>Builds the timing points used by standard-mode map export.</summary>
    /// <param name="timing">The original beatmap timing data.</param>
    /// <param name="events">The standard-mode events to place in the map.</param>
    /// <returns>A new timing-point list containing the exported redlines and inherited points.</returns>
    public IReadOnlyList<TimingPoint> BuildStandardTimingPoints(
        Timing timing,
        IReadOnlyList<HitsoundEvent> events)
    {
        ArgumentNullException.ThrowIfNull(timing);
        ArgumentNullException.ThrowIfNull(events);

        var changes = timing.Redlines
            .Select(point => new TimingPointChange(
                point.Copy(),
                true,
                true,
                uninherited: true,
                omitFirstBarLine: true,
                fuzziness: 0.4))
            .ToList();
        foreach (var item in events)
        {
            var point = timing.GetTimingPointAtTime(item.Time + 5)?.Copy() ?? new TimingPoint();
            point.Offset = item.Time;
            point.SampleIndex = item.CustomIndex;
            point.Volume = Math.Round(point.Volume * item.Volume);
            changes.Add(new TimingPointChange(point, index: true, volume: true));
        }

        Timing exported = new(timing.SliderMultiplier);
        TimingPointChange.Apply(exported, changes);
        return exported.ToList();
    }

    /// <summary>
    ///     Groups layer events whose timestamps are within the legacy leniency.
    /// </summary>
    /// <param name="layers">The editable layers to group.</param>
    /// <param name="defaultSample">The normal sample inserted into addition-only packages.</param>
    /// <param name="leniency">Maximum absolute timestamp distance in milliseconds.</param>
    /// <param name="needNormalSample">Whether addition-only packages need a normal fallback.</param>
    /// <returns>Chronologically ordered packages with copied sample arguments.</returns>
    public IReadOnlyList<SamplePackage> ZipLayers(
        IEnumerable<HitsoundLayer> layers,
        Sample defaultSample,
        double leniency = 15,
        bool needNormalSample = true)
    {
        ArgumentNullException.ThrowIfNull(layers);
        ArgumentNullException.ThrowIfNull(defaultSample);
        if (!double.IsFinite(leniency) || leniency < 0) throw new ArgumentOutOfRangeException(nameof(leniency));

        List<SamplePackage> packages = [];
        foreach (var layer in layers)
        {
            ArgumentNullException.ThrowIfNull(layer);
            foreach (double time in layer.Times ?? [])
            {
                var package = packages.FirstOrDefault(candidate => Math.Abs(candidate.Time - time) <= leniency);
                if (package is null)
                    packages.Add(new SamplePackage(
                        time,
                        [new Sample(layer)]));
                else
                    package.Samples.Add(new Sample(layer));
            }
        }

        if (needNormalSample)
            // Packages without a hitnormal sample
            foreach (var package in packages.Where(package =>
                         package.Samples.All(sample => sample.Hitsound != Hitsound.Normal)))
                package.Samples.Add(defaultSample.Copy());

        return packages.OrderBy(package => package.Time).ToArray();
    }

    /// <summary>
    ///     Moves gain into osu!'s event volume while retaining the legacy roughness rules.
    /// </summary>
    /// <param name="packages">Packages to mutate.</param>
    /// <param name="roughness">Quantization step for generated sample volumes.</param>
    /// <param name="alwaysFullVolume">Whether generated samples must use unity gain.</param>
    /// <param name="individualVolume">Whether each package sample keeps its own event volume.</param>
    public void BalanceVolumes(
        IEnumerable<SamplePackage> packages,
        double roughness,
        bool alwaysFullVolume,
        bool individualVolume = false)
    {
        ArgumentNullException.ThrowIfNull(packages);
        if (!double.IsFinite(roughness) || roughness < 0) throw new ArgumentOutOfRangeException(nameof(roughness));

        foreach (var package in packages)
        {
            if (package.Samples.Count == 0) continue;

            if (individualVolume)
            {
                // Simply mix the volume in the sample to the outside volume
                foreach (var sample in package.Samples)
                {
                    sample.OutsideVolume = AudioVolume.FromAmplitude(
                        AudioVolume.ToAmplitude(sample.OutsideVolume) * AudioVolume.ToAmplitude(sample.SampleArgs.Volume));
                    sample.SampleArgs.Volume = 1;
                }

                continue;
            }

            double maxVolume = package.Samples.Max(sample => sample.SampleArgs.Volume);
            if (NearlyEqual(maxVolume, -0.01)) maxVolume = 1;

            foreach (var sample in package.Samples)
            {
                if (NearlyEqual(sample.SampleArgs.Volume, -0.01)) sample.SampleArgs.Volume = 1;

                // Pick the new volume such that the samples have a volume as high as possible and the greenline brings the volume down.
                // With this equation the final amplitude stays the same while the greenline has the volume of the loudest sample at this time.
                double newVolume = AudioVolume.FromAmplitude(
                    AudioVolume.ToAmplitude(sample.OutsideVolume) * AudioVolume.ToAmplitude(sample.SampleArgs.Volume) / AudioVolume.ToAmplitude(maxVolume));

                if (Math.Abs(newVolume - 1) > roughness && !alwaysFullVolume)
                    // If roughness is not 0 it will quantize the new volume in order to reduce the number of different volumes
                    sample.SampleArgs.Volume = roughness > 0
                        ? roughness * Math.Round(newVolume / roughness)
                        : newVolume;
                else
                    sample.SampleArgs.Volume = 1;
            }

            // Assuming the volume of the sample is always maximum, this equation makes sure that
            // the loudest sample at this time has the wanted amplitude using the volume change from the greenline.
            package.SetAllOutsideVolume(alwaysFullVolume
                ? AudioVolume.FromAmplitude(
                    AudioVolume.ToAmplitude(package.MaxOutsideVolume) * AudioVolume.ToAmplitude(maxVolume))
                : maxVolume);
        }
    }

    /// <summary>
    ///     Builds custom-index assignments and events for standard-mode export.
    /// </summary>
    /// <param name="packages">Balanced packages in chronological order.</param>
    /// <param name="previousSchema">Optional schema loaded from a previous project.</param>
    /// <param name="allowGrowth">Whether new source mixes may receive new indices.</param>
    /// <param name="firstCustomIndex">First index assigned when making a schema.</param>
    /// <param name="isSampleValid">Source validation policy.</param>
    /// <param name="comparer">Source identity policy.</param>
    /// <returns>Events and the schema used for those events.</returns>
    public HitsoundStudioStandardResult BuildStandard(
        IEnumerable<SamplePackage> packages,
        SampleSchema? previousSchema,
        bool allowGrowth,
        int firstCustomIndex,
        Func<SampleGeneratingArgs, bool> isSampleValid,
        SampleGeneratingArgsComparer? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(packages);
        ArgumentNullException.ThrowIfNull(isSampleValid);
        if (firstCustomIndex < 0) throw new ArgumentOutOfRangeException(nameof(firstCustomIndex));

        var packageList = packages.ToList();
        var identity = comparer ?? new SampleGeneratingArgsComparer();
        var indices = packageList
            .Select(package => package.GetCustomIndex(identity))
            .ToList();
        foreach (var index in indices) index.CleanInvalids(isSampleValid);

        var previousIndices = previousSchema?.GetCustomIndices(identity);
        List<CustomIndex> schemaIndices;
        if (previousIndices is null)
            schemaIndices = GiveIndices(Optimize(indices), false, firstCustomIndex);
        else if (allowGrowth)
            schemaIndices = GiveIndices(
                Optimize(previousIndices.Concat(indices).ToList()),
                true,
                firstCustomIndex);
        else
            schemaIndices = previousIndices;

        var events = MatchPackages(packageList, indices, schemaIndices);
        return new HitsoundStudioStandardResult(events, new SampleSchema(schemaIndices));
    }

    /// <summary>
    ///     Builds named events for coinciding and storyboard modes.
    /// </summary>
    /// <param name="packages">Balanced packages in chronological order.</param>
    /// <param name="previousSchema">Optional singleton-name schema to reuse.</param>
    /// <param name="maniaPositions">Whether positions should represent mania keys.</param>
    /// <param name="includeRegularHitsounds">Whether events retain osu! sample families.</param>
    /// <param name="allowGrowth">Whether names absent from the schema may be added.</param>
    /// <param name="isSampleValid">Source validation policy.</param>
    /// <param name="comparer">Source identity policy.</param>
    /// <returns>Named events, source names, and generated positions.</returns>
    public HitsoundStudioNamedResult BuildNamed(
        IEnumerable<SamplePackage> packages,
        SampleSchema? previousSchema,
        bool maniaPositions,
        bool includeRegularHitsounds,
        bool allowGrowth,
        Func<SampleGeneratingArgs, bool> isSampleValid,
        SampleGeneratingArgsComparer? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(packages);
        ArgumentNullException.ThrowIfNull(isSampleValid);

        var packageList = packages.ToList();
        var identity = comparer ?? new SampleGeneratingArgsComparer();
        HashSet<SampleGeneratingArgs> allSamples = new(
            packageList.SelectMany(package => package.Samples.Select(sample => sample.SampleArgs)),
            identity);
        var names = previousSchema?.GetSampleNames(identity)
                    ?? new Dictionary<SampleGeneratingArgs, string>(identity);
        var usedNames = names.Values.Where(name => !string.IsNullOrEmpty(name)).ToHashSet(StringComparer.Ordinal);

        foreach (var sample in allSamples)
        {
            if (names.ContainsKey(sample)) continue;

            // Validate the sample because we expect only valid samples to be present in the sample schema
            if (!isSampleValid(sample))
            {
                names[sample] = string.Empty;
                continue;
            }

            if (!allowGrowth && previousSchema is not null)
                throw new InvalidDataException(
                    $"Given sample schema doesn't support sample ({sample}) and growth is disabled.");

            string baseName = sample.GetFilename();
            string name = baseName;
            int suffix = 1;
            while (!usedNames.Add(name)) name = $"{baseName}-{++suffix}";

            names[sample] = name;
        }

        var positions = maniaPositions
            ? GenerateManiaPositions(allSamples, identity)
            : GeneratePositions(allSamples, identity);
        List<HitsoundEvent> events = [];
        foreach (var package in packageList)
        foreach (var sample in package.Samples)
        {
            string filename = names.TryGetValue(sample.SampleArgs, out string? value)
                ? value
                : string.Empty;
            var position = positions[sample.SampleArgs];
            events.Add(includeRegularHitsounds
                ? new HitsoundEvent(
                    package.Time,
                    position,
                    sample.OutsideVolume,
                    filename,
                    sample.SampleSet,
                    sample.SampleSet,
                    0,
                    sample.Hitsound == Hitsound.Whistle,
                    sample.Hitsound == Hitsound.Finish,
                    sample.Hitsound == Hitsound.Clap)
                : new HitsoundEvent(
                    package.Time,
                    position,
                    sample.OutsideVolume,
                    filename,
                    SampleSet.None,
                    SampleSet.None,
                    0,
                    false,
                    false,
                    false));
        }

        return new HitsoundStudioNamedResult(events, new SampleSchema(names), names, positions);
    }

    /// <summary>Creates a schema from the supplied names without copying invalid entries.</summary>
    /// <param name="names">Source names keyed by generation arguments.</param>
    /// <returns>A persisted schema containing non-empty names.</returns>
    public SampleSchema CreateSchema(IEnumerable<KeyValuePair<SampleGeneratingArgs, string>> names)
    {
        return new SampleSchema(names.ToDictionary(pair => pair.Key, pair => pair.Value));
    }

    /// <summary>Generates standard osu! playfield positions for unique samples.</summary>
    /// <param name="samples">The distinct generation arguments.</param>
    /// <param name="comparer">The generation-argument comparer.</param>
    /// <returns>One deterministic position per sample.</returns>
    public Dictionary<SampleGeneratingArgs, Vector2> GeneratePositions(
        IEnumerable<SampleGeneratingArgs> samples,
        SampleGeneratingArgsComparer? comparer = null)
    {
        var values = samples.ToArray();
        int spacingX = 128;
        int spacingY = 128;
        bool reduceX = false;
        while ((int)(512d / spacingX + 1) * (int)(384d / spacingY + 1) < values.Length && spacingX > 1)
        {
            reduceX = !reduceX;
            if (reduceX) spacingX /= 2;
            else spacingY /= 2;
        }

        Dictionary<SampleGeneratingArgs, Vector2> positions = new(comparer ?? new SampleGeneratingArgsComparer());
        int x = 0;
        int y = 0;
        foreach (var value in values)
        {
            positions[value] = new Vector2(x, y);
            x += spacingX;
            if (x > 512)
            {
                x = 0;
                y += spacingY;
                if (y > 384) y = 0;
            }
        }

        return positions;
    }

    /// <summary>Generates one centered column position per unique sample.</summary>
    /// <param name="samples">The distinct generation arguments.</param>
    /// <param name="comparer">The generation-argument comparer.</param>
    /// <returns>One deterministic mania position per sample.</returns>
    public Dictionary<SampleGeneratingArgs, Vector2> GenerateManiaPositions(
        IEnumerable<SampleGeneratingArgs> samples,
        SampleGeneratingArgsComparer? comparer = null)
    {
        var values = samples.ToArray();
        int keys = Math.Clamp(values.Length, 1, 18);
        Dictionary<SampleGeneratingArgs, Vector2> positions = new(comparer ?? new SampleGeneratingArgsComparer());
        double x = 256d / keys;
        foreach (var value in values)
        {
            positions[value] = new Vector2(Math.Round(x), 192);
            x += 512d / keys;
            if (x > 512) x = 256d / keys;
        }

        return positions;
    }

    private static List<CustomIndex> Optimize(List<CustomIndex> indices)
    {
        List<CustomIndex> optimized = [];
        // Try merging together CustomIndices as much as possible
        foreach (var index in indices)
        {
            var merge = optimized.FirstOrDefault(candidate => candidate.CanMerge(index));
            if (merge is null) optimized.Add(index.Copy());
            else merge.MergeWith(index);
        }

        // Remove any CustomIndices that might be obsolete
        optimized.RemoveAll(subject => !indices.Any(candidate =>
            subject.Fits(candidate)
            && optimized.Where(other => !ReferenceEquals(other, subject))
                .All(other => !other.Fits(candidate))));
        return optimized;
    }

    private static List<CustomIndex> GiveIndices(
        List<CustomIndex> indices,
        bool keepExisting,
        int firstCustomIndex)
    {
        int next = firstCustomIndex;
        var used = indices.Where(index => index.Index >= 0).Select(index => index.Index).ToHashSet();
        foreach (var index in indices)
        {
            if (keepExisting && index.Index >= 0) continue;
            while (used.Contains(next)) next++;
            index.Index = next++;
            used.Add(index.Index);
        }

        return indices;
    }

    private static List<HitsoundEvent> MatchPackages(
        IReadOnlyList<SamplePackage> packages,
        IReadOnlyList<CustomIndex> packageIndices,
        IReadOnlyList<CustomIndex> schemaIndices)
    {
        List<HitsoundEvent> events = [];
        int packageIndex = 0;
        while (packageIndex < packages.Count)
        {
            // Find CustomIndex that fits the most packages from here
            CustomIndex? best = null;
            int bestCount = 0;
            foreach (var candidate in schemaIndices)
            {
                int count = 0;
                while (packageIndex + count < packageIndices.Count && candidate.Fits(packageIndices[packageIndex + count])) count++;
                if (count > bestCount)
                {
                    best = candidate;
                    bestCount = count;
                }
            }

            if (best is null || bestCount == 0)
                throw new InvalidDataException(
                    "Custom indices can't fit the sample packages. Maybe the previous sample schema is incompatible or growth is disabled.");

            // Add all the fitted packages as hitsounds
            for (int offset = 0; offset < bestCount; offset++) events.Add(packages[packageIndex + offset].GetHitsound(best.Index));

            packageIndex += bestCount;
        }

        return events;
    }

    private static bool NearlyEqual(double left, double right)
    {
        return Math.Abs(left - right) < 1e-9;
    }
}
