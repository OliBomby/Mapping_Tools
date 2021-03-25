using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools_Core.BeatmapHelper;
using Mapping_Tools_Core.MathUtil;

namespace Mapping_Tools_Core.Tools.ComboColourStudio {
    public class ColourHaxImporter {
        public int MaxBurstLength { get; set; } = 1;

        public ComboColourProject ImportColourHax(Beatmap beatmap) {
            var comboColours = beatmap.ComboColoursList;
            var colourPoints = new List<IColourPoint>();

            // Get all the hit objects which can colorhax. AKA new combos and not spinners
            var colorHaxObjects = beatmap.HitObjects.Where(o => o.ActualNewCombo && !o.IsSpinner).ToArray();

            // Get the array with all the lengths of sequences that are going to be checked
            var sequenceLengthChecks = Enumerable.Range(1, comboColours.Count * 2 + 2).ToArray();

            int sequenceStartIndex = 0;
            int[] lastNormalSequence = null;
            bool lastBurst = false;
            while (sequenceStartIndex < colorHaxObjects.Length) {
                var firstComboHitObject = colorHaxObjects[sequenceStartIndex];

                var bestSequence = GetBestSequenceAtIndex(
                    sequenceStartIndex,
                    3,
                    colorHaxObjects,
                    beatmap,
                    sequenceLengthChecks,
                    lastBurst,
                    lastNormalSequence
                )?.Item1;

                if (bestSequence == null) {
                    lastBurst = false;
                    sequenceStartIndex += 1;
                    continue;
                }

                var bestContribution = GetSequenceContribution(colorHaxObjects, sequenceStartIndex, bestSequence);

                // Get the colours for every colour index. Using modulo to make sure the index is always in range.
                var colourSequence = bestSequence;

                // Add a new colour point
                var mode = bestContribution == 1 &&
                           GetComboLength(beatmap.HitObjects, firstComboHitObject) <= MaxBurstLength
                    ? ColourPointMode.Burst
                    : ColourPointMode.Normal;

                // To optimize on colour points, we dont add a new colour point if the previous point was a burst and
                // the sequence before the burst is equivalent to this sequence
                if (!(lastBurst && lastNormalSequence != null && IsSubSequence(bestSequence, lastNormalSequence) &&
                      (bestSequence.Length == lastNormalSequence.Length || bestContribution <= bestSequence.Length))) {
                    colourPoints.Add(new ColourPoint(firstComboHitObject.StartTime, mode, colourSequence));
                }

                lastBurst = mode == ColourPointMode.Burst;
                sequenceStartIndex += bestContribution;
                lastNormalSequence = mode == ColourPointMode.Burst ? lastNormalSequence : bestSequence;
            }

            return new ComboColourProject(colourPoints, comboColours, MaxBurstLength);
        }

        private Tuple<int[], int, double> GetBestSequenceAtIndex(int sequenceStartIndex, int depth, IReadOnlyList<HitObject> colorHaxObjects, Beatmap beatmap, int[] sequenceLengthChecks, bool lastBurst, int[] lastNormalSequence) {
            if (sequenceStartIndex >= colorHaxObjects.Count) {
                return null;
            }

            var firstComboHitObject = colorHaxObjects[sequenceStartIndex];

            // Getting all sequences and calculating the scores
            var sequences = sequenceLengthChecks.Select(n => GetColourSequence(colorHaxObjects, sequenceStartIndex, n)).ToArray();
            var contributions = sequences.Select(s => GetSequenceContribution(colorHaxObjects, sequenceStartIndex, s)).ToArray();

            // Get the sequence with the highest score
            double bestScore = double.NegativeInfinity;
            int[] bestSequence = null;
            int bestContribution = 0;
            double bestCost = double.PositiveInfinity;
            for (int i = 0; i < sequences.Length; i++) {
                var sequence = sequences[i];

                if (sequence == null) {
                    continue;
                }

                var contribution = contributions[i];

                var burst = contribution == 1 &&
                            GetComboLength(beatmap.HitObjects, firstComboHitObject) <= MaxBurstLength;

                double cost = sequence.Length;

                // There is no cost if the colour point doesnt have to be added
                if (lastBurst && lastNormalSequence != null && IsSubSequence(sequence, lastNormalSequence) &&
                    (sequence.Length == lastNormalSequence.Length || contribution <= sequence.Length)) {
                    cost = 0;
                }

                // Recursively add the cost and contribution to this cost and contribution
                if (depth > 0) {
                    var nextBest = GetBestSequenceAtIndex(
                        sequenceStartIndex + contribution,
                        depth - 1,
                        colorHaxObjects,
                        beatmap,
                        sequenceLengthChecks,
                        burst,
                        burst ? lastNormalSequence : sequence
                    );

                    if (nextBest != null) {
                        contribution += nextBest.Item2 / 2;
                        cost += nextBest.Item3 / 2;
                    }
                }

                // Factor the contribution over the cost
                var score = contribution / cost;

                if (bestSequence != null && (score < bestScore || Math.Abs(score - bestScore) < Precision.DOUBLE_EPSILON && cost >= bestCost)) continue;

                bestScore = score;
                bestSequence = sequence;
                bestContribution = contribution;
                bestCost = cost;

                if (double.IsPositiveInfinity(bestScore)) {
                    break;
                }
            }

            return new Tuple<int[], int, double>(bestSequence, bestContribution, bestCost);
        }

        public static bool IsSubSequence(int[] sequence, int[] biggerSequence) {
            if (biggerSequence == null || sequence.Length > biggerSequence.Length) {
                return false;
            }

            return !sequence.Where((t, i) => t != biggerSequence[i]).Any();
        }

        private static int GetComboLength(List<HitObject> hitObjects, HitObject firstHitObject) {
            var index = hitObjects.IndexOf(firstHitObject);
            var count = 1;
            while (++index < hitObjects.Count && !hitObjects[index].ActualNewCombo) {
                count++;
            }

            return count;
        }

        private static int[] GetColourSequence(IReadOnlyList<HitObject> hitObjects, int startIndex, int sequenceLength) {
            int[] colourSequence = new int[sequenceLength];

            for (int i = 0; i < sequenceLength; i++) {
                if (startIndex + i >= hitObjects.Count) {
                    return null;
                }

                colourSequence[i] = hitObjects[startIndex + i].ColourIndex;
            }

            return colourSequence;
        }

        private static int GetSequenceContribution(IReadOnlyList<HitObject> hitObjects, int startIndex, IReadOnlyList<int> colourSequence) {
            if (colourSequence == null) {
                return 0;
            }

            int index = startIndex;
            int sequenceIndex = 0;
            int score = 0;

            while (index < hitObjects.Count && hitObjects[index].ColourIndex == colourSequence[sequenceIndex]) {
                score++;
                index++;
                sequenceIndex = MathHelper.Mod(sequenceIndex + 1, colourSequence.Count);
            }

            return score;
        }
    }
}