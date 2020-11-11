using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.ToolHelpers;
using Mapping_Tools.Components.Domain;

namespace Mapping_Tools.Classes.Tools.ComboColourStudio {
    public class ComboColourProject : BindableBase {
        private ObservableCollection<ColourPoint> _colourPoints;
        private ObservableCollection<SpecialColour> _comboColours;

        private int _maxBurstLength;

        public ComboColourProject() {
            ColourPoints = new ObservableCollection<ColourPoint>();
            ComboColours = new ObservableCollection<SpecialColour>();

            MaxBurstLength = 1;

            AddColourPointCommand = new CommandImplementation(_ => {
                double time = ColourPoints.Count > 1 ? 
                    ColourPoints.Count(o => o.IsSelected) > 0 ? ColourPoints.Where(o => o.IsSelected).Max(o => o.Time) :
                    ColourPoints.Last().Time
                    : 0;
                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) {
                    try {
                        time = EditorReaderStuff.GetEditorTime();
                    } catch (Exception ex) {
                        ex.Show();
                    }
                }
                
                ColourPoints.Add(GenerateNewColourPoint(time));
            });

            RemoveColourPointCommand = new CommandImplementation(_ => {
                if (ColourPoints.Any(o => o.IsSelected)) {
                    ColourPoints.RemoveAll(o => o.IsSelected);
                    return;
                }
                if (ColourPoints.Count > 0) {
                    ColourPoints.RemoveAt(ColourPoints.Count - 1);
                }
            });

            AddComboCommand = new CommandImplementation(_ => {
                if (ComboColours.Count >= 8) return;
                ComboColours.Add(ComboColours.Count > 0
                    ? new SpecialColour(ComboColours[ComboColours.Count - 1].Color, $"Combo{ComboColours.Count + 1}")
                    : new SpecialColour(Colors.White, $"Combo{ComboColours.Count + 1}"));
            });

            RemoveComboCommand = new CommandImplementation(_ => {
                if (ComboColours.Count > 0) {
                    ComboColours.RemoveAt(ComboColours.Count - 1);
                }
            });
        }

        private void ComboColoursOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            /*if (e.OldItems != null) {
                foreach (var oldItem in e.OldItems) {
                    var removed = (SpecialColour) oldItem;
                    foreach (var colourPoint in ColourPoints) {
                        colourPoint.ColourSequence.Remove(removed);
                    }
                }
            }*/

            MatchComboColourReferences();
        }

        private void ColourPointsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            if (e.OldItems != null) {
                foreach (var oldItem in e.OldItems) {
                    ((ColourPoint) oldItem).ParentProject = null;
                }
            }
            if (e.NewItems == null) return;
            foreach (var newItem in e.NewItems) {
                var newColourPoint = (ColourPoint) newItem;
                newColourPoint.ParentProject = this;
                // Match object references with the combo colours
                MatchComboColourReferences();
            }
        }

        /// <summary>
        /// This method makes sure the SpecialColour objects in the colour sequences are the same objects as in the combo colours.
        /// With this the colours of the colour sequences update when the combo colours get changed.
        /// </summary>
        private void MatchComboColourReferences() {
            foreach (var colourPoint in ColourPoints) {
                for (int i = 0; i < colourPoint.ColourSequence.Count; i++) {
                    colourPoint.ColourSequence[i] =
                        ComboColours.FirstOrDefault(o => o.Name == colourPoint.ColourSequence[i].Name) ??
                        colourPoint.ColourSequence[i];
                }
            }
        }

        private ColourPoint GenerateNewColourPoint(double time = 0, IEnumerable<SpecialColour> colours = null, ColourPointMode mode = ColourPointMode.Normal) {
            return new ColourPoint(time, colours ?? new SpecialColour[0], mode, this);
        }

        public void ImportColourHaxFromBeatmap(string importPath) {
            try {
                var editor = new BeatmapEditor(importPath);
                var beatmap = editor.Beatmap;

                // Add default colours if there are no colours
                if (beatmap.ComboColours.Count == 0) {
                    beatmap.ComboColours.AddRange(ComboColour.GetDefaultComboColours());
                }

                ComboColours.Clear();
                for (int i = 0; i < beatmap.ComboColours.Count; i++) {
                    ComboColours.Add(new SpecialColour(beatmap.ComboColours[i].Color, $"Combo{i + 1}"));
                }

                // Remove all colour points since those are getting replaced
                ColourPoints.Clear();

                // Get all the hit objects which can colorhax. AKA new combos and not spinners
                var colorHaxObjects = beatmap.HitObjects.Where(o => o.ActualNewCombo && !o.IsSpinner).ToArray();

                // Get the array with all the lengths of sequences that are going to be checked
                var sequenceLengthChecks = Enumerable.Range(1, ComboColours.Count * 2 + 2).ToArray();

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
                    var colourSequence = bestSequence.Select(o => ComboColours[MathHelper.Mod(o, ComboColours.Count)]);

                    // Add a new colour point
                    var mode = bestContribution == 1 &&
                               GetComboLength(beatmap.HitObjects, firstComboHitObject) <= MaxBurstLength
                        ? ColourPointMode.Burst
                        : ColourPointMode.Normal;

                    // To optimize on colour points, we dont add a new colour point if the previous point was a burst and
                    // the sequence before the burst is equivalent to this sequence
                    if (!(lastBurst && lastNormalSequence != null && IsSubSequence(bestSequence, lastNormalSequence) && 
                          (bestSequence.Length == lastNormalSequence.Length || bestContribution <= bestSequence.Length))) {
                        ColourPoints.Add(GenerateNewColourPoint(firstComboHitObject.Time, colourSequence, mode));
                    }

                    lastBurst = mode == ColourPointMode.Burst;
                    sequenceStartIndex += bestContribution;
                    lastNormalSequence = mode == ColourPointMode.Burst ? lastNormalSequence : bestSequence;
                }
            }
            catch( Exception ex ) {
                ex.Show();
            }
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

        public void ImportComboColoursFromBeatmap(string importPath) {
            try {
                var editor = new BeatmapEditor(importPath);
                var beatmap = editor.Beatmap;

                ComboColours.Clear();
                for (int i = 0; i < beatmap.ComboColours.Count; i++) {
                    ComboColours.Add(new SpecialColour(beatmap.ComboColours[i].Color, $"Combo{i + 1}"));
                }
            }
            catch( Exception ex ) {
                ex.Show();
            }
        }

        
        public ObservableCollection<ColourPoint> ColourPoints {
            get => _colourPoints;
            set { Set(ref _colourPoints, value);
                ColourPoints.CollectionChanged += ColourPointsOnCollectionChanged;
            }
        }

        public ObservableCollection<SpecialColour> ComboColours {
            get => _comboColours;
            set { Set(ref _comboColours, value);
                ComboColours.CollectionChanged += ComboColoursOnCollectionChanged;
            }
        }

        public int MaxBurstLength {
            get => _maxBurstLength;
            set => Set(ref _maxBurstLength, value);
        }

        public CommandImplementation AddColourPointCommand { get; }
        public CommandImplementation RemoveColourPointCommand { get; }
        public CommandImplementation AddComboCommand { get; }
        public CommandImplementation RemoveComboCommand { get; }
    }
}