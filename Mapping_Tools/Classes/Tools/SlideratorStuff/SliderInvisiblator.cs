using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.MathUtil;
using System;

namespace Mapping_Tools.Classes.Tools.SlideratorStuff {
    public static class SliderInvisiblator {
        public static int SNAPTOL => 50000;

        public static (Vector2[], long) Invisiblate(HitObject ho, Timing timing, Vector2[] sbPositions = null) {
            int timeLength = (int)Math.Round(timing.CalculateSliderTemporalLength(ho.Time, ho.PixelLength));

            if (sbPositions == null) sbPositions = ho.SliderPath.SliderballPositions(timeLength);

            // Round all positions
            for (int i = 0; i < sbPositions.Length; i++) {
                sbPositions[i].Round();
            }

            return Invisiblate(timeLength, sbPositions);
        }
        
        public static (Vector2[], long) Invisiblate(int duration, Vector2[] sbPositions) {
            Vector2[] controlPoints = new Vector2[15 + 5 * (duration - 1)];
            Vector2 startpos = sbPositions[0].Rounded();
            long frameDist = (long)(2 * 67141632 + 2 * 33587200 + startpos.X + startpos.Y - sbPositions[1].X - sbPositions[1].Y);

            // Zigzagging to maintain invisibility during snaking process
            controlPoints[0] = sbPositions[0];
            controlPoints[1] = new Vector2(4196352, 0) + startpos;
            controlPoints[2] = new Vector2(4196352, 2099200) + startpos;
            controlPoints[3] = new Vector2(8392704, 2099200) + startpos;
            controlPoints[4] = new Vector2(8392704, 4198400) + startpos;
            controlPoints[5] = new Vector2(16785408, 4198400) + startpos;
            controlPoints[6] = new Vector2(16785408, 8396800) + startpos;
            controlPoints[7] = new Vector2(33570816, 8396800) + startpos;
            controlPoints[8] = new Vector2(33570816, 16793600) + startpos;
            controlPoints[9] = new Vector2(67141632, 16793600) + startpos;
            controlPoints[10] = new Vector2(67141632, 33587200 + SNAPTOL) + startpos;
            controlPoints[11] = new Vector2(67141632 + startpos.X, sbPositions[1].Y);
            controlPoints[12] = new Vector2(sbPositions[1].X, sbPositions[1].Y);

            int ctrlPtIdx = 13;
            for (int i = 2; i < duration + 1; i++) {
                controlPoints[ctrlPtIdx] = new Vector2(67141632 + startpos.X, sbPositions[i - 1].Y);
                controlPoints[ctrlPtIdx + 1] = new Vector2(67141632 + startpos.X,
                    Math.Round(33587200 + 0.5 * (startpos.Y - startpos.X + sbPositions[i - 1].X + sbPositions[i].X +
                    sbPositions[i - 1].Y + sbPositions[i].Y - sbPositions[1].X - sbPositions[1].Y)));
                controlPoints[ctrlPtIdx + 2] = new Vector2(67141632 + startpos.X, sbPositions[i].Y);
                controlPoints[ctrlPtIdx + 3] = new Vector2(4 * SNAPTOL, sbPositions[i].Y);
                controlPoints[ctrlPtIdx + 4] = new Vector2(sbPositions[i].X, sbPositions[i].Y);
                ctrlPtIdx += 5;
            }

            // Add extra segment of length 0 to end for rendering purposes
            Vector2 lastPt = sbPositions[sbPositions.Length - 1];
            controlPoints[ctrlPtIdx] = new Vector2(lastPt.X, lastPt.Y);
            controlPoints[ctrlPtIdx + 1] = new Vector2(lastPt.X, lastPt.Y);

            return (controlPoints, frameDist);
        }
    }
}
