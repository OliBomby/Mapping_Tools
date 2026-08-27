using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.ComboColourStudio.Models;

namespace Mapping_Tools.Core.Tools.ComboColourStudio;

/// <summary>
///     Applies Combo Colour Studio projects and infers projects from existing maps.
/// </summary>
public static class ComboColourStudioEngine
{
    /// <summary>Applies palette colours and combo skips to a mutable beatmap.</summary>
    /// <param name="beatmap">The beatmap to mutate.</param>
    /// <param name="project">The validated project to apply.</param>
    public static void Apply(Beatmap beatmap, ComboColourEngineOptions project)
    {
        ArgumentNullException.ThrowIfNull(beatmap);
        Validate(project);

        var colourPoints = project.ColourPoints.OrderBy(point => point.Time).ToList();
        // Collection order is the osu! palette order. Names are stable
        // sequence references, not a second ordering instruction.
        var comboColours = project.ComboColours.ToList();
        beatmap.ComboColours = project.ComboColours
            .Select(colour => new ComboColour(colour.Color))
            .ToList();

        if (beatmap.HitObjects.Count == 0 || colourPoints.Count == 0) return;

        int lastColourPointColourIndex = -1;
        var lastColourPoint = colourPoints[0];
        int lastColourIndex = 0;
        List<ColourPoint> exceptions = [];

        foreach (var newCombo in beatmap.HitObjects.Where(objectToColour =>
                     objectToColour.ActualNewCombo && !objectToColour.IsSpinner))
        {
            int comboLength = GetComboLength(newCombo, beatmap.HitObjects);
            var colourPoint = GetColourPoint(
                colourPoints,
                newCombo.Time,
                exceptions,
                comboLength <= project.MaxBurstLength);
            var colourSequence = colourPoint.ColourSequence.ToList();

            if (colourPoint.Mode == ColourPointMode.Burst) exceptions.Add(colourPoint);

            lastColourPointColourIndex = lastColourPointColourIndex == -1 || lastColourPoint.Equals(colourPoint)
                ? lastColourPointColourIndex
                : colourSequence.FindIndex(colour => colour.Name == comboColours[lastColourIndex].Name);

            int colourPointColourIndex = lastColourPointColourIndex == -1 || colourSequence.Count == 0
                ? 0
                : lastColourPoint.Equals(colourPoint)
                    ? MathHelper.Mod(lastColourPointColourIndex + 1, colourSequence.Count)
                    : lastColourPointColourIndex == 0 && colourSequence.Count > 1
                        ? 1
                        : 0;

            int colourIndex = colourSequence.Count == 0
                ? MathHelper.Mod(lastColourIndex + 1, comboColours.Count)
                : comboColours.FindIndex(colour =>
                    colour.Name == colourSequence[colourPointColourIndex].Name);
            if (colourIndex == -1)
                throw new ArgumentException(
                    $"Can not use colour {colourSequence[colourPointColourIndex].Name} "
                    + $"of colour point at offset {colourPoint.Time} because it does not exist in the combo colours.",
                    nameof(project));

            int comboIncrease = MathHelper.Mod(colourIndex - lastColourIndex, project.ComboColours.Count);
            newCombo.ComboSkip = MathHelper.Mod(comboIncrease - 1, project.ComboColours.Count);
            if (!newCombo.NewCombo && newCombo.ComboSkip != 0) newCombo.NewCombo = true;

            lastColourPointColourIndex = colourPointColourIndex;
            lastColourPoint = colourPoint;
            lastColourIndex = colourIndex;
        }
    }

    /// <summary>Validates a Combo Colour Studio project before beatmap mutation.</summary>
    /// <param name="project">The project settings to validate.</param>
    /// <exception cref="ArgumentNullException"><paramref name="project" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    ///     A palette is empty, contains an invalid or duplicate name, a colour
    ///     point has a non-finite offset, or a point references a missing colour.
    /// </exception>
    public static void Validate(ComboColourEngineOptions project)
    {
        ArgumentNullException.ThrowIfNull(project);
        if (project.MaxBurstLength < 0)
            throw new ArgumentOutOfRangeException(
                nameof(project.MaxBurstLength),
                project.MaxBurstLength,
                "Max burst length cannot be negative.");

        if (project.ComboColours.Count == 0)
            throw new ArgumentException(
                "Add at least one combo colour before running the tool.",
                nameof(project.ComboColours));

        string?[] names = project.ComboColours.Select(colour => colour.Name).ToArray();
        if (names.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException(
                "Every combo colour must have a name.",
                nameof(project.ComboColours));

        if (names.Where(name => !string.IsNullOrWhiteSpace(name))
            .GroupBy(name => name, StringComparer.Ordinal)
            .Any(group => group.Count() > 1))
            throw new ArgumentException(
                "Combo colour names must be unique.",
                nameof(project.ComboColours));

        HashSet<string?> nameSet = new(names, StringComparer.Ordinal);
        foreach (var point in project.ColourPoints)
        {
            if (!double.IsFinite(point.Time))
                throw new ArgumentException(
                    "Every colour point offset must be a finite number.",
                    nameof(project.ColourPoints));

            foreach (var colour in point.ColourSequence)
                if (!nameSet.Contains(colour.Name))
                    throw new ArgumentException(
                        $"Colour point at offset {point.Time} references missing colour '{colour.Name}'.",
                        nameof(project.ColourPoints));
        }
    }

    /// <summary>Extracts the combo palette from a beatmap, using the legacy names.</summary>
    /// <param name="beatmap">The source beatmap.</param>
    /// <returns>A new project containing the extracted palette and no colour points.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="beatmap" /> is <see langword="null" />.</exception>
    public static ComboColourEngineOptions ImportComboColours(Beatmap beatmap)
    {
        ArgumentNullException.ThrowIfNull(beatmap);

        // Add default colours if there are no colours
        ComboColour[] colours = beatmap.ComboColours.Count == 0
            ? ComboColour.GetDefaultComboColours()
            : beatmap.ComboColours.ToArray();

        ComboColourEngineOptions result = new();
        for (int index = 0; index < colours.Length; index++)
            result.ComboColours.Add(new SpecialColour(colours[index].Color, $"Combo{index + 1}"));

        return result;
    }

    /// <summary>Extracts a palette and infers normal and burst points from a beatmap's combo skips.</summary>
    /// <param name="beatmap">The source beatmap.</param>
    /// <param name="maxBurstLength">The largest combo eligible for burst points.</param>
    /// <returns>A new project containing the extracted palette and inferred colour points.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="beatmap" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxBurstLength" /> is negative.</exception>
    public static ComboColourEngineOptions ImportColourHax(Beatmap beatmap, int maxBurstLength)
    {
        ArgumentNullException.ThrowIfNull(beatmap);

        if (maxBurstLength < 0)
            throw new ArgumentOutOfRangeException(nameof(maxBurstLength));

        ComboColourEngineOptions project = ImportComboColours(beatmap);
        project.MaxBurstLength = maxBurstLength;

        // Remove all colour points since those are getting replaced
        project.ColourPoints.Clear();

        // Get all the hit objects which can colorhax. AKA new combos and not spinners
        HitObject[] objects = beatmap.HitObjects
            .Where(hitObject => hitObject.ActualNewCombo && !hitObject.IsSpinner)
            .ToArray();
        // Get the array with all the lengths of sequences that are going to be checked
        int[] sequenceLengthChecks = Enumerable.Range(1, project.ComboColours.Count * 2 + 2).ToArray();
        int sequenceStartIndex = 0;
        int[]? lastNormalSequence = null;
        bool lastBurst = false;
        while (sequenceStartIndex < objects.Length)
        {
            var firstComboHitObject = objects[sequenceStartIndex];
            var bestSequence = GetBestSequenceAtIndex(
                sequenceStartIndex,
                3,
                objects,
                beatmap,
                sequenceLengthChecks,
                project.MaxBurstLength,
                lastBurst,
                lastNormalSequence);
            int[]? sequence = bestSequence?.Item1;
            if (sequence is null)
            {
                lastBurst = false;
                sequenceStartIndex++;
                continue;
            }

            int contribution = GetSequenceContribution(objects, sequenceStartIndex, sequence);
            // Get the colours for every colour index. Using modulo to make sure the index is always in range.
            var colours = sequence.Select(index =>
                project.ComboColours[MathHelper.Mod(index, project.ComboColours.Count)]);
            var mode = contribution == 1 && GetComboLengthForImport(beatmap.HitObjects, firstComboHitObject) <= project.MaxBurstLength
                ? ColourPointMode.Burst
                : ColourPointMode.Normal;
            // Add a new colour point
            // To optimize on colour points, we dont add a new colour point if the previous point was a burst and
            // the sequence before the burst is equivalent to this sequence
            if (!(lastBurst
                  && lastNormalSequence is not null
                  && ComboColourProjectIsSubSequence(sequence, lastNormalSequence)
                  && (sequence.Length == lastNormalSequence.Length || contribution <= sequence.Length)))
                project.AddColourPoint(firstComboHitObject.Time, colours, mode);

            lastBurst = mode == ColourPointMode.Burst;
            sequenceStartIndex += contribution;
            lastNormalSequence = mode == ColourPointMode.Burst ? lastNormalSequence : sequence;
        }

        return project;
    }

    /// <summary>Builds a bounded preview of the colours selected for map combos without mutating the map.</summary>
    /// <param name="beatmap">The source beatmap.</param>
    /// <param name="project">The project to preview.</param>
    /// <param name="maximumItems">Maximum number of preview entries.</param>
    /// <returns>Preview entries in map order.</returns>
    public static IReadOnlyList<ComboColourPreviewEntry> BuildPreview(
        Beatmap beatmap,
        ComboColourEngineOptions project,
        int maximumItems = 256)
    {
        ArgumentNullException.ThrowIfNull(beatmap);
        ArgumentNullException.ThrowIfNull(project);
        if (maximumItems < 0) throw new ArgumentOutOfRangeException(nameof(maximumItems));

        if (maximumItems == 0) return [];

        List<ComboColourPreviewEntry> result = [];
        foreach (var point in project.ColourPoints.OrderBy(point => point.Time))
        foreach (var colour in point.ColourSequence)
        {
            result.Add(new ComboColourPreviewEntry(
                point.Time,
                point.Mode,
                colour.Name ?? string.Empty,
                colour.Color));
            if (result.Count >= maximumItems) return result;
        }

        return result;
    }

    /// <summary>Tests whether a sequence is the prefix of a larger sequence.</summary>
    /// <param name="sequence">The candidate prefix.</param>
    /// <param name="biggerSequence">The sequence to inspect.</param>
    /// <returns><see langword="true" /> when every candidate element matches.</returns>
    public static bool IsSubSequence(IReadOnlyList<int> sequence, IReadOnlyList<int>? biggerSequence)
    {
        if (biggerSequence is null || sequence.Count > biggerSequence.Count) return false;

        return !sequence.Where((value, index) => value != biggerSequence[index]).Any();
    }

    private static bool ComboColourProjectIsSubSequence(int[] sequence, int[] biggerSequence)
    {
        return IsSubSequence(sequence, biggerSequence);
    }

    private static Tuple<int[], int, double>? GetBestSequenceAtIndex(
        int sequenceStartIndex,
        int depth,
        IReadOnlyList<HitObject> objects,
        Beatmap beatmap,
        IReadOnlyList<int> sequenceLengthChecks,
        int maxBurstLength,
        bool lastBurst,
        int[]? lastNormalSequence)
    {
        if (sequenceStartIndex >= objects.Count) return null;

        var firstComboHitObject = objects[sequenceStartIndex];
        // Getting all sequences and calculating the scores
        int[][] sequences = sequenceLengthChecks
            .Select(length => GetColourSequence(objects, sequenceStartIndex, length))
            .ToArray()!;
        int[] contributions = sequences
            .Select(sequence => GetSequenceContribution(objects, sequenceStartIndex, sequence))
            .ToArray();
        // Get the sequence with the highest score
        double bestScore = double.NegativeInfinity;
        int[]? bestSequence = null;
        int bestContribution = 0;
        double bestCost = double.PositiveInfinity;
        for (int index = 0; index < sequences.Length; index++)
        {
            int[]? sequence = sequences[index];
            if (sequence is null) continue;

            int contribution = contributions[index];
            bool burst = contribution == 1 && GetComboLengthForImport(beatmap.HitObjects, firstComboHitObject) <= maxBurstLength;
            double cost = sequence.Length;
            // There is no cost if the colour point doesnt have to be added
            if (lastBurst
                && lastNormalSequence is not null
                && ComboColourProjectIsSubSequence(sequence, lastNormalSequence)
                && (sequence.Length == lastNormalSequence.Length || contribution <= sequence.Length))
                cost = 0;

            // Recursively add the cost and contribution to this cost and contribution
            if (depth > 0)
            {
                var next = GetBestSequenceAtIndex(
                    sequenceStartIndex + contribution,
                    depth - 1,
                    objects,
                    beatmap,
                    sequenceLengthChecks,
                    maxBurstLength,
                    burst,
                    burst ? lastNormalSequence : sequence);
                if (next is not null)
                {
                    contribution += next.Item2 / 2;
                    cost += next.Item3 / 2;
                }
            }

            // Factor the contribution over the cost
            double score = contribution / cost;
            if (bestSequence is not null && (score < bestScore || Math.Abs(score - bestScore) < Precision.DOUBLE_EPSILON && cost >= bestCost))
                continue;

            bestScore = score;
            bestSequence = sequence;
            bestContribution = contribution;
            bestCost = cost;
            if (double.IsPositiveInfinity(bestScore)) break;
        }

        return bestSequence is null ? null : new Tuple<int[], int, double>(bestSequence, bestContribution, bestCost);
    }

    private static int GetComboLength(HitObject first, IReadOnlyList<HitObject> objects)
    {
        int index = -1;
        for (int objectIndex = 0; objectIndex < objects.Count; objectIndex++)
            if (ReferenceEquals(objects[objectIndex], first))
            {
                index = objectIndex;
                break;
            }

        if (index == -1) return 0;

        int count = 1;
        while (++index < objects.Count && !objects[index].NewCombo) count++;

        return count;
    }

    private static int GetComboLength(IReadOnlyList<HitObject> objects, HitObject first)
    {
        return GetComboLength(first, objects);
    }

    private static int GetComboLengthForImport(IReadOnlyList<HitObject> objects, HitObject first)
    {
        int index = -1;
        for (int objectIndex = 0; objectIndex < objects.Count; objectIndex++)
            if (ReferenceEquals(objects[objectIndex], first))
            {
                index = objectIndex;
                break;
            }

        if (index == -1) return 0;

        int count = 1;
        while (++index < objects.Count && !objects[index].ActualNewCombo) count++;

        return count;
    }

    private static int[]? GetColourSequence(IReadOnlyList<HitObject> objects, int startIndex, int length)
    {
        int[] sequence = new int[length];
        for (int index = 0; index < length; index++)
        {
            if (startIndex + index >= objects.Count) return null;

            sequence[index] = objects[startIndex + index].ColourIndex;
        }

        return sequence;
    }

    private static int GetSequenceContribution(
        IReadOnlyList<HitObject> objects,
        int startIndex,
        IReadOnlyList<int>? sequence)
    {
        if (sequence is null) return 0;

        int index = startIndex;
        int sequenceIndex = 0;
        int score = 0;
        while (index < objects.Count && objects[index].ColourIndex == sequence[sequenceIndex])
        {
            score++;
            index++;
            sequenceIndex = MathHelper.Mod(sequenceIndex + 1, sequence.Count);
        }

        return score;
    }

    private static ColourPoint GetColourPoint(
        IReadOnlyList<ColourPoint> points,
        double time,
        IReadOnlyCollection<ColourPoint> exceptions,
        bool includeBurst)
    {
        return points.Except(exceptions).LastOrDefault(point =>
                   point.Time <= time + 5 && (point.Mode != ColourPointMode.Burst || point.Time >= time - 5 && includeBurst))
               ?? points.Except(exceptions).FirstOrDefault(point => point.Mode != ColourPointMode.Burst) ?? points[0];
    }
}
