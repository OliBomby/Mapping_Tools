using Mapping_Tools.Classes.MathUtil;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Mapping_Tools.Classes.Tools.SlideratorStuff;

public static class SliderInvisiblator
{
    public static int Snaptol => (int)Math.Pow(2, 5) * 3;

    public static (Vector2[], double) Invisiblate(int duration, Vector2[] sbPositions, double globalSv = 1.4)
    {
        // Before rounding sbPositions, calculate starting coordinate for each ms' final segment to make the sliderball rotate appropriately
        Vector2[] msLastSegStart = new Vector2[duration + 1];
        double ang;
        // We don't care about msLastSegStart[0] so we'll leave it at 0. Technically we could save one Vector2's worth of space here but it would make indexing harder to read than necessary.
        // Find the first angle - we can't calculate the angle between points that are the same, but the sliderball's rotation should be the same as it was before.
        double savedAng = 0;
        for (int i = 1; i < duration + 1; i++) {
            if (sbPositions[0] != sbPositions[i]) {
                savedAng = Math.Atan2(sbPositions[i - 1].Y - sbPositions[i].Y, sbPositions[i - 1].X - sbPositions[i].X);
            }
        }
        for (int i = 1; i < duration + 1; i++) {
            if (sbPositions[i - 1] == sbPositions[i]) {
                ang = savedAng;
            }
            else {
                ang = Math.Atan2(sbPositions[i - 1].Y - sbPositions[i].Y, sbPositions[i - 1].X - sbPositions[i].X);
                savedAng = ang;
            }
            msLastSegStart[i] = new Vector2((float)(Snaptol * Math.Cos(ang) + (float)sbPositions[i].Rounded().X), (float)(Snaptol * Math.Sin(ang) + (float)sbPositions[i].Rounded().Y));
        }

        // Round all positions to float precision values
        for (int i = 0; i < sbPositions.Length; i++) {
            sbPositions[i].Round();
            sbPositions[i] = new Vector2((float)sbPositions[i].X, (float)sbPositions[i].Y);
        }

        Vector2[] controlPoints = new Vector2[8 + 4 * (duration - 1)];
        Vector2 maxXY = new Vector2(768, 412);

        List<Vector2> curMsPath = new List<Vector2>();
        // First ms travel adds SNAPTOL
        curMsPath.Add(sbPositions[0]);
        curMsPath.Add(new Vector2((float)(67141632 + maxXY.X), (float)sbPositions[0].Y));
        curMsPath.Add(new Vector2((float)(67141632 + maxXY.X), (float)(33587200 - (Snaptol / 6) + maxXY.Y)));
        curMsPath.Add(new Vector2((float)(67141632 + maxXY.X), (float)msLastSegStart[1].Y));
        curMsPath.Add(msLastSegStart[1]);
        curMsPath.Add(sbPositions[1]);

        // The precision of bpm calculation might be important when trying to be this precise with virtual sliderball position. Although the bpm is stored as a G17, it's written to the .osu as a G15 because that's the default for ToString().
        // So we will be using G15 to not fuck people over in the editor as they use this tool and continue mapping.

        double frameDist = OsuStableDistance(curMsPath) - 2 * Snaptol / 3;

        double MpB = 100 * globalSv / frameDist;
        MpB = double.Parse(MpB.ToString());

        frameDist = 100 * globalSv / MpB;


        curMsPath.ToArray().CopyTo(controlPoints, 0);

        int ctrlPtIdx = 6;
        double correction = 0;

        for (int i = 2; i < duration + 1; i++) {
            curMsPath.Clear();
            // The first point on this path is the last point of the previous path
            curMsPath.Add(sbPositions[i - 1]);

            // verticalTravel tells us how far down we need to go before going over and back up
            double verticalTravel = correction + frameDist - (Math.Abs(sbPositions[i - 1].X - msLastSegStart[i].X) + (sbPositions[i - 1].Y - msLastSegStart[i].Y) + Snaptol);

            curMsPath.Add(new Vector2((float)sbPositions[i - 1].X, (float)(sbPositions[i - 1].Y + verticalTravel / 2)));
            if (sbPositions[i - 1].X != msLastSegStart[i].X) {
                curMsPath.Add(new Vector2((float)msLastSegStart[i].X, (float)(sbPositions[i - 1].Y + verticalTravel / 2)));
            }
            curMsPath.Add(msLastSegStart[i]);
            curMsPath.Add(sbPositions[i]);

            // Here we calculate what osu! finds for the distance travelled here, so that we can correct for it on the next iteration.
            double pathDist = OsuStableDistance(curMsPath);
            correction += frameDist - pathDist;

            // Copy curMsPath into controlPoints. We use ctrlPtIdx-1 because we have the last point of the previous path in this path as well.
            curMsPath.ToArray().CopyTo(controlPoints, ctrlPtIdx - 1);

            // Update ctrlPtIdx
            ctrlPtIdx += curMsPath.Count - 1;
        }
        Vector2[] newControlPoints = new Vector2[ctrlPtIdx+2];
        Array.Copy(controlPoints, newControlPoints, ctrlPtIdx);

        // Add extra segment of length 0 to end for sliderend snapping abuse
        Vector2 lastPt = sbPositions[duration];
        newControlPoints[ctrlPtIdx] = new Vector2((float)lastPt.X, (float)lastPt.Y);
        newControlPoints[ctrlPtIdx + 1] = new Vector2((float)lastPt.X, (float)lastPt.Y);

        return (newControlPoints, frameDist);
    }

    private static double OsuStableDistance(List<Vector2> controlPoints)
    {
        double length = 0;
        Vector2 cp, lp;
        float num1, num2, num3;
        for (int i = 1; i < controlPoints.Count; i++) {
            lp = controlPoints.ElementAt(i - 1);
            cp = controlPoints.ElementAt(i);
            num1 = (float)Math.Round(lp.X)- (float)Math.Round(cp.X);
            num2 = (float)Math.Round(lp.Y) - (float)Math.Round(cp.Y);
            num3 = num1 * num1 + num2 * num2;

            length += (float)Math.Sqrt(num3);
        }
        return length;
    }
}