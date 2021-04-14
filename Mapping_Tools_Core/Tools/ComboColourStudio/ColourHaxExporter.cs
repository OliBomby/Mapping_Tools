using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools_Core.BeatmapHelper;
using Mapping_Tools_Core.MathUtil;

namespace Mapping_Tools_Core.Tools.ComboColourStudio {
    public class ColourHaxExporter {
        public static void ExportColourHax(IComboColourProject project, Beatmap beatmap) {
            var orderedColourPoints = project.ColourPoints.OrderBy(o => o.Time).ToList();
            var comboColours = project.ComboColours.ToList();

            // Setting the combo colours
            beatmap.ComboColoursList = new List<IComboColour>(project.ComboColours);

            // Setting the combo skips
            if (beatmap.HitObjects.Count > 0 && orderedColourPoints.Count > 0) {
                int lastColourPointColourIndex = -1;
                var lastColourPoint = orderedColourPoints[0];
                int lastColourIndex = 0;
                var exceptions = new List<IColourPoint>();
                foreach (var newCombo in beatmap.HitObjects.Where(o => o.ActualNewCombo && !o.IsSpinner)) {
                    int comboLength = GetComboLength(newCombo, beatmap.HitObjects);
                    //Console.WriteLine(comboLength);

                    // Get the colour point for this new combo
                    var colourPoint = GetColourPointAtTime(orderedColourPoints, newCombo.StartTime, exceptions, comboLength <= project.MaxBurstLength);
                    var colourSequence = colourPoint.ColourSequence.ToList();

                    // Add the colour point to the exceptions so it doesnt get used again
                    if (colourPoint.Mode == ColourPointMode.Burst) {
                        exceptions.Add(colourPoint);
                    }

                    // Get the last colour index on the sequence of this colour point
                    // In case the colour point changed
                    lastColourPointColourIndex = lastColourPointColourIndex == -1 || lastColourPoint.Equals(colourPoint) ?
                        lastColourPointColourIndex :
                        colourSequence.FindIndex(o => o == lastColourIndex);

                    // Get the next colour index on this colour point
                    // Check if colourSequence count is 0 to prevent division by 0
                    var colourPointColourIndex = lastColourPointColourIndex == -1 || colourSequence.Count == 0
                        ? 0
                        : lastColourPoint.Equals(colourPoint) ?
                        MathHelper.Mod(lastColourPointColourIndex + 1, colourSequence.Count) :
                        // If the colour point changed try going back to index 0
                        lastColourPointColourIndex == 0 && colourSequence.Count > 1 ? 1 : 0;

                    //Console.WriteLine("colourPointColourIndex: " + colourPointColourIndex);
                    //Console.WriteLine("colourPointColour: " + colourPoint.ColourSequence[colourPointColourIndex].Name);

                    // Find the combo index of the chosen colour in the sequence
                    // Check if the colourSequence count is 0 to prevent an out-of-range exception
                    var colourIndex = colourSequence.Count == 0 ? MathHelper.Mod(lastColourIndex + 1, comboColours.Count) :
                        colourSequence[colourPointColourIndex];

                    if (colourIndex == -1) {
                        throw new ArgumentException($"Can not use colour {colourSequence[colourPointColourIndex]} of colour point at offset {colourPoint.Time} because it does not exist in the combo colours.");
                    }

                    //Console.WriteLine("colourIndex: " + colourIndex);

                    var comboIncrease = MathHelper.Mod(colourIndex - lastColourIndex, comboColours.Count);

                    // Do -1 combo skip since it always does +1 combo colour for each new combo which is not on a spinner
                    newCombo.ComboSkip = MathHelper.Mod(comboIncrease - 1, comboColours.Count);

                    // Set new combo to true for the case this is the first object and new combo is false
                    if (!newCombo.NewCombo && newCombo.ComboSkip != 0) {
                        newCombo.NewCombo = true;
                    }

                    //Console.WriteLine("comboSkip: " + newCombo.ComboSkip);

                    lastColourPointColourIndex = colourPointColourIndex;
                    lastColourPoint = colourPoint;
                    lastColourIndex = colourIndex;
                }
            }
        }

        private static int GetComboLength(HitObject newCombo, IList<HitObject> hitObjects) {
            int count = 1;
            var index = hitObjects.IndexOf(newCombo);

            if (index == -1) {
                return 0;
            }

            while (++index < hitObjects.Count) {
                var hitObject = hitObjects[index];
                if (hitObject.NewCombo) {
                    return count;
                }

                count++;
            }

            return count;
        }

        private static IColourPoint GetColourPointAtTime(IReadOnlyList<IColourPoint> colourPoints, double time, IReadOnlyCollection<IColourPoint> exceptions, bool includeBurst) {
            return colourPoints.Except(exceptions).LastOrDefault(o => o.Time <= time + 5 && (o.Mode != ColourPointMode.Burst || o.Time >= time - 5 && includeBurst)) ??
                   colourPoints.Except(exceptions).FirstOrDefault(o => o.Mode != ColourPointMode.Burst) ??
                   colourPoints[0];
        }
    }
}