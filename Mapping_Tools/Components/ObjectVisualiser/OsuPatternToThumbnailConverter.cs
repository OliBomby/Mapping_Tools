using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using Mapping_Tools.Classes;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.BeatmapHelper.SliderPathStuff;
using Mapping_Tools.Classes.Tools.PatternGallery;

namespace Mapping_Tools.Components.ObjectVisualiser {
    public class OsuPatternToThumbnailConverter : IMultiValueConverter {
        private const int ThumbnailWidth = 150;
        private const int ThumbnailHeight = 110;
        private const int Margin = 10;
        private const float Scale = (ThumbnailWidth - Margin * 2) / 512f;
        private const float PenWidth = 0.15f;
        private const float CircleSizeFactor = 1 / (1 + PenWidth);
        
        private const double MaxPixelLength = 1e6;
        private const int MaxAnchorCount = 5000;

        private static readonly Dictionary<string, BitmapSource> Cache = new();

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            // Value is a string for the pattern filename. The parameter is the OsuPatternFileHandler
            if (values.Length < 2 || values[0] is not string filename || values[1] is not OsuPatternFileHandler fileHandler) {
                return null;
            }

            // Check if the pattern is in the cache
            if (Cache.TryGetValue(filename, out var cachedBitmap)) {
                return cachedBitmap;
            }

            var bitmapSource = DrawBeatmapFromFile(filename, fileHandler);
            Cache.Add(filename, bitmapSource);
            return bitmapSource;
        }

        private BitmapSource DrawBeatmapFromFile(string filename, OsuPatternFileHandler fileHandler) {
            try {
                // Load the beatmap
                var beatmap = fileHandler.GetPatternBeatmap(filename);

                // Calculate and cache some slider paths
                var sliderPaths = new Dictionary<HitObject, SliderPath>();
                foreach (var hitObject in beatmap.HitObjects.Where(hitObject => hitObject.IsSlider && hitObject.PixelLength < MaxPixelLength && hitObject.CurvePoints.Count < MaxAnchorCount)) {
                    var sliderPath = hitObject.GetSliderPath();
                    sliderPaths[hitObject] = sliderPath;
                    hitObject.EndPos = sliderPath.PositionAt(1);
                }

                beatmap.UpdateStacking();

                // Draw the thumbnail
                using var bmp = new Bitmap(ThumbnailWidth, ThumbnailHeight);
                using var gfx = Graphics.FromImage(bmp);

                // Draw one thousand random white lines on a dark blue background
                gfx.SmoothingMode = SmoothingMode.AntiAlias;
                gfx.Clear(Color.Black);
                gfx.TranslateTransform(Margin, Margin);
                gfx.ScaleTransform(Scale, Scale);

                if (beatmap.HitObjects.Count <= 0) {
                    return bmp.ToBitmapSource();
                }

                var firstTime = beatmap.HitObjects[0].Time;
                const double approachTime = 1000;
                var circleSize = Beatmap.GetHitObjectRadius(beatmap.Difficulty["CircleSize"].DoubleValue);
                var hitObjects = beatmap.HitObjects.TakeWhile(o => o.Time < firstTime + approachTime).Reverse();
                using var pen = new Pen(Color.White, (float) circleSize * PenWidth);

                foreach (var hitObject in hitObjects) {
                    DrawHitObject(gfx, hitObject, circleSize, pen, sliderPaths);
                }

                return bmp.ToBitmapSource();
            } catch {
                return null;
            }
        }

        private void DrawHitObject(Graphics gfx, HitObject hitObject, double circleSize, Pen pen, Dictionary<HitObject, SliderPath> sliderPaths) {
            var pos = hitObject.StackedPos;
            var c = CircleSizeFactor * circleSize;
            var x = (int) (pos.X - c);
            var y = (int) (pos.Y - c);
            var s = (int) (c * 2);
            if (hitObject.IsSlider) {
                if (!sliderPaths.ContainsKey(hitObject)) return;

                var outlinePen = new Pen(Color.White, (float) circleSize * 1.95f) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round };
                var insidePen = new Pen(Color.Black, (float) circleSize * 1.65f) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round };
                var path = sliderPaths[hitObject];

                using GraphicsPath gc = new GraphicsPath();
                var points = path.CalculatedPath.Select(p => new PointF((float) p.X, (float) p.Y)).ToArray();
                gc.AddLines(points);

                gfx.DrawPath(outlinePen, gc);
                gfx.DrawPath(insidePen, gc);
                DrawCircleAtProgress(gfx, pen, path, 1, c);
                DrawCircleAtProgress(gfx, pen, path, 0, c);
            } else if (hitObject.IsSpinner) {
                gfx.DrawEllipse(pen, 256 - 150, 192 - 150, 300, 300);
                gfx.DrawEllipse(pen, 256 - 5, 192 - 5, 10, 10);
            } else {
                gfx.DrawEllipse(pen, x, y, s, s);
            }
        }

        private void DrawCircleAtProgress(Graphics gfx, Pen pen, SliderPath path, double progress, double circleSize) {
            var pos = path.PositionAt(progress);
            var x = (int) (pos.X - circleSize);
            var y = (int) (pos.Y - circleSize);
            var s = (int) (circleSize * 2);
            gfx.DrawEllipse(pen, x, y, s, s);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            throw new NotSupportedException("OsuPatternToThumbnailConverter is a OneWay converter.");
        }
    }
}