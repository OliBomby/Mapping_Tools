using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.MathUtil;
using System;

namespace Mapping_Tools.Classes.Tools.SlideratorStuff {
    public static class SliderInvisiblator {
        public static int SNAPTOL => (int)Math.Pow(2,20);

        public static (Vector2[], long) Invisiblate(HitObject ho, Timing timing, Vector2[] sbPositions = null) {
            ho.CalculateSliderTemporalLength(timing, false);
            int timeLength = (int)Math.Round(ho.TemporalLength);

            if (sbPositions == null) sbPositions = ho.SliderPath.SliderballPositions(timeLength);

            // Round all positions
            for (int i = 0; i < sbPositions.Length; i++) {
                sbPositions[i].Round();
            }

            return Invisiblate(timeLength, sbPositions);
        }
        
        public static (Vector2[], long) Invisiblate(int duration, Vector2[] sbPositions) {

            // Round all positions
            for (int i = 0; i < sbPositions.Length; i++) {
                sbPositions[i].Round();
            }

            Vector2[] controlPoints = new Vector2[14 + 7 * (duration - 1)];
            Vector2 maxXY = new Vector2(768, 412);
            long frameDist = (long)(2 * 67141632 + 2 * 33587200 + 2 * maxXY.X + 2 * maxXY.Y - sbPositions[0].X - sbPositions[0].Y - sbPositions[1].X - sbPositions[1].Y);

            // Zigzagging to maintain invisibility during snaking process
            controlPoints[0] = sbPositions[0];
            controlPoints[1] = new Vector2(4196352, 0) + new Vector2(maxXY.X, sbPositions[0].Y);
            controlPoints[2] = new Vector2(4196352, 2099200) + maxXY;
            controlPoints[3] = new Vector2(8392704, 2099200) + maxXY;
            controlPoints[4] = new Vector2(8392704, 4198400) + maxXY;
            controlPoints[5] = new Vector2(16785408, 4198400) + maxXY;
            controlPoints[6] = new Vector2(16785408, 8396800) + maxXY;
            controlPoints[7] = new Vector2(33570816, 8396800) + maxXY;
            controlPoints[8] = new Vector2(33570816, 16793600) + maxXY;
            controlPoints[9] = new Vector2(67141632, 16793600) + maxXY;
            controlPoints[10] = new Vector2(67141632, 33587200 + SNAPTOL) + maxXY;
            controlPoints[11] = new Vector2(67141632, 0) + new Vector2(maxXY.X, sbPositions[1].Y);
            controlPoints[12] = new Vector2(4 * SNAPTOL, sbPositions[1].Y);
            controlPoints[13] = new Vector2(sbPositions[1].X, sbPositions[1].Y);

            int ctrlPtIdx = 14;
            for (int i = 2; i < duration + 1; i++) {
                // Move to a safely small position to add length early. It's OK if we lose or gain 2px here or there as long as it's compatible with floats.
                int leftover = (int) Math.Round((frameDist - (2 * 67141632 + 2 * 33587200 + 2 * maxXY.X + 2 * maxXY.Y - sbPositions[i - 1].X - sbPositions[i - 1].Y - sbPositions[i].X - sbPositions[i].Y)) / 2);
                if (leftover > 0) {
                    controlPoints[ctrlPtIdx] = sbPositions[i - 1] + new Vector2(0, leftover);
                    controlPoints[ctrlPtIdx + 1] = sbPositions[i - 1];
                    controlPoints[ctrlPtIdx + 2] = new Vector2(67141632, 0) + new Vector2(maxXY.X, sbPositions[i - 1].Y);
                    controlPoints[ctrlPtIdx + 3] = new Vector2(67141632, 33587200) + maxXY;
                    controlPoints[ctrlPtIdx + 4] = new Vector2(67141632, 0) + new Vector2(maxXY.X, sbPositions[i].Y);
                    controlPoints[ctrlPtIdx + 5] = new Vector2(4 * SNAPTOL, sbPositions[i].Y);
                    controlPoints[ctrlPtIdx + 6] = new Vector2(sbPositions[i].X, sbPositions[i].Y);
                    ctrlPtIdx += 7;
                }
                else {
                    controlPoints[ctrlPtIdx] = new Vector2(67141632, 0) + new Vector2(maxXY.X, sbPositions[i - 1].Y);
                    controlPoints[ctrlPtIdx + 1] = new Vector2(67141632, 33587200 + leftover) + maxXY;
                    controlPoints[ctrlPtIdx + 2] = new Vector2(67141632, 0) + new Vector2(maxXY.X, sbPositions[i].Y);
                    controlPoints[ctrlPtIdx + 3] = new Vector2(4 * SNAPTOL, sbPositions[i].Y);
                    controlPoints[ctrlPtIdx + 4] = new Vector2(sbPositions[i].X, sbPositions[i].Y);
                    ctrlPtIdx += 5;
                }
                
            }
            Vector2[] newControlPoints = new Vector2[ctrlPtIdx + 2];
            Array.Copy(controlPoints, newControlPoints, ctrlPtIdx);

            // Add extra segment of length 0 to end for rendering purposes
            Vector2 lastPt = sbPositions[sbPositions.Length - 1];
            newControlPoints[ctrlPtIdx] = new Vector2(lastPt.X, lastPt.Y);
            newControlPoints[ctrlPtIdx + 1] = new Vector2(lastPt.X, lastPt.Y);

            return (newControlPoints, frameDist);
        }
    }
}
