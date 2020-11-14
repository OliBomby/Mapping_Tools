using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.BeatmapHelper.SliderPathStuff;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.ToolHelpers
{
    /// <summary>
    /// Converts a slider between differnet types of curves.
    /// </summary>
    public static class BezierConverter
    {
        private static readonly List<CircleBezierPreset> CirclePresets = new List<CircleBezierPreset> {
            new CircleBezierPreset(0.4993379862754501, GetPoints("1.0:0.0|1.0:0.2549893626632736|0.8778997558480327:0.47884446188920726")),
            new CircleBezierPreset(1.7579419829169447, GetPoints("1.0:0.0|1.0:0.6263026|0.42931178:1.0990661|-0.18605515:0.9825393")),
            new CircleBezierPreset(3.1385246920140215, GetPoints("1.0:0.0|1.0:0.87084764|0.002304826:1.5033062|-0.9973236:0.8739115|-0.9999953:0.0030679568")),
            new CircleBezierPreset(5.69720464620727, GetPoints("1.0:0.0|1.0:1.4137783|-1.4305235:2.0779421|-2.3410065:-0.94017583|0.05132711:-1.7309346|0.8331702:-0.5530167")),
            new CircleBezierPreset(2 * Math.PI, GetPoints("1.0:0.0|1.0:1.2447058|-0.8526471:2.118367|-2.6211002:7.854936e-06|-0.8526448:-2.118357|1.0:-1.2447058|1.0:-2.4492937e-16"))};

        private static List<Vector2> GetPoints(string str)
        {
            string[] strPoints = str.Split('|');
            List<Vector2> points = new List<Vector2>(strPoints.Length);
            foreach (string strPoint in strPoints)
            {
                string[] strCoords = strPoint.Split(':');
                points.Add(new Vector2(float.Parse(strCoords[0], CultureInfo.InvariantCulture), float.Parse(strCoords[1], CultureInfo.InvariantCulture)));
            }
            return points;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sliderPath"></param>
        /// <returns></returns>
        public static SliderPath ConvertToBezier(SliderPath sliderPath)
        {
            switch (sliderPath.Type)
            {
                case PathType.Linear:
                    return ConvertLinearToBezier(sliderPath);
                case PathType.PerfectCurve:
                    return ConvertCircleToBezier(sliderPath);
                case PathType.Catmull:
                    return ConvertCatmullToBezier(sliderPath);
                case PathType.Bezier:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return sliderPath;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="perfectPath"></param>
        /// <returns></returns>
        public static SliderPath ConvertCircleToBezier(SliderPath perfectPath)
        {
            if (perfectPath.Type != PathType.PerfectCurve)
            {
                return perfectPath;
            }
            Vector2[] newAnchors = ConvertCircleToBezierAnchors(perfectPath.ControlPoints).ToArray();

            SliderPath newPath = new SliderPath(PathType.Bezier, newAnchors, perfectPath.ExpectedDistance);
            return newPath;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ca"></param>
        /// <returns></returns>
        public static SliderPath ConvertCircleToBezier(CircleArc ca)
        {
            Vector2[] newAnchors = ConvertCircleToBezierAnchors(ca).ToArray();

            SliderPath newPath = new SliderPath(PathType.Bezier, newAnchors);
            return newPath;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="perfectAnchors"></param>
        /// <returns></returns>
        public static SliderPath ConvertCircleToBezier(List<Vector2> perfectAnchors)
        {
            Vector2[] newAnchors = ConvertCircleToBezierAnchors(perfectAnchors).ToArray();

            SliderPath newPath = new SliderPath(PathType.Bezier, newAnchors);
            return newPath;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="perfectAnchors"></param>
        /// <returns></returns>
        public static List<Vector2> ConvertCircleToBezierAnchors(List<Vector2> perfectAnchors)
        {
            CircleArc cs = new CircleArc(perfectAnchors);
            if (!cs.Stable)
                return perfectAnchors;
            return ConvertCircleToBezierAnchors(cs);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cs"></param>
        /// <returns></returns>
        public static List<Vector2> ConvertCircleToBezierAnchors(CircleArc cs)
        {
            CircleBezierPreset preset = CirclePresets.Last();
            foreach (CircleBezierPreset CBP in CirclePresets)
            {
                if (CBP.MaxAngle >= cs.ThetaRange)
                {
                    preset = CBP;
                    break;
                }
            }

            List<Vector2> arc = preset.Points.Copy();
            double arcLength = preset.MaxAngle;

            // Converge on arcLength of thetaRange
            int n = arc.Count - 1;
            double tf = cs.ThetaRange / arcLength;
            while (Math.Abs(tf - 1) > 0.0000001)
            {
                for (int j = 0; j < n; j++)
                {
                    for (int i = n; i > j; i--)
                    {
                        arc[i] = arc[i] * tf + arc[i - 1] * (1 - tf);
                    }
                }
                arcLength = Math.Atan2(arc.Last()[1], arc.Last()[0]);
                if (arcLength < 0)
                {
                    arcLength += 2 * Math.PI;
                }
                tf = cs.ThetaRange / arcLength;
            }

            // Adjust rotation, radius, and position
            Matrix2 rotator = cs.Rotator;
            for (int i = 0; i < arc.Count; i++)
            {
                arc[i] = Matrix2.Mult(rotator, arc[i]) + cs.Centre;
            }
            return arc;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="catmullPath"></param>
        /// <returns></returns>
        public static SliderPath ConvertCatmullToBezier(SliderPath catmullPath)
        {
            if (catmullPath.Type != PathType.Catmull)
            {
                return catmullPath;
            }
            Vector2[] newAnchors = ConvertCatmullToBezierAnchors(catmullPath.ControlPoints).ToArray();

            SliderPath newPath = new SliderPath(PathType.Bezier, newAnchors, catmullPath.ExpectedDistance);
            return newPath;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="catmullAnchors"></param>
        /// <returns></returns>
        public static SliderPath ConvertCatmullToBezier(List<Vector2> catmullAnchors)
        {
            Vector2[] newAnchors = ConvertCatmullToBezierAnchors(catmullAnchors).ToArray();

            SliderPath newPath = new SliderPath(PathType.Bezier, newAnchors);
            return newPath;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pts"></param>
        /// <returns></returns>
        public static List<Vector2> ConvertCatmullToBezierAnchors(List<Vector2> pts)
        {
            List<Vector2> cubics = new List<Vector2>
            {
                pts[0]
            };
            int iLen = pts.Count;
            for (int i = 0; i < iLen - 1; i++)
            {
                var v1 = i > 0 ? pts[i - 1] : pts[i];
                var v2 = pts[i];
                var v3 = i < pts.Length() - 1 ? pts[i + 1] : v2 + v2 - v1;
                var v4 = i < pts.Length() - 2 ? pts[i + 2] : v3 + v3 - v2;

                cubics.Add((-v1 + 6 * v2 + v3) / 6);
                cubics.Add((-v4 + 6 * v3 + v2) / 6);
                cubics.Add(v3);
                cubics.Add(v3);
            }
            cubics.RemoveAt(cubics.Count - 1);
            return cubics;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="linearPath"></param>
        /// <returns></returns>
        public static SliderPath ConvertLinearToBezier(SliderPath linearPath)
        {
            if (linearPath.Type != PathType.Linear)
            {
                return linearPath;
            }
            Vector2[] newAnchors = ConvertLinearToBezierAnchors(linearPath.ControlPoints).ToArray();

            SliderPath newPath = new SliderPath(PathType.Bezier, newAnchors, linearPath.ExpectedDistance);
            return newPath;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="linearAnchors"></param>
        /// <returns></returns>
        public static SliderPath ConvertLinearToBezier(List<Vector2> linearAnchors)
        {
            Vector2[] newAnchors = ConvertLinearToBezierAnchors(linearAnchors).ToArray();

            SliderPath newPath = new SliderPath(PathType.Bezier, newAnchors);
            return newPath;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pts"></param>
        /// <returns></returns>
        public static List<Vector2> ConvertLinearToBezierAnchors(List<Vector2> pts)
        {
            List<Vector2> bezier = new List<Vector2>
            {
                pts[0]
            };
            int iLen = pts.Count;
            for (int i = 1; i < iLen; i++)
            {
                bezier.Add(pts[i]);
                bezier.Add(pts[i]);
            }
            bezier.RemoveAt(bezier.Count - 1);
            return bezier;
        }
    }
}
