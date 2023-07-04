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
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.Tools.PatternGallery;

namespace Mapping_Tools.Components.ObjectVisualiser {
    public class OsuPatternToThumbnailConverter : IMultiValueConverter {
        private const int ThumbnailWidth = 150;
        private const int ThumbnailHeight = 110;
        private const int Margin = 10;
        private const float Scale = (ThumbnailWidth - Margin * 2) / 512f;
        private const float PenWidth = 0.15f;
        
        private const double MaxPixelLength = 1e6;
        private const int MaxAnchorCount = 5000;

        private static readonly Brush circleInsideBrush = Brushes.Green;
        private static readonly Brush circleOutsideBrush = Brushes.White;
        private static readonly Brush sliderInsideBrush = Brushes.DarkSlateGray;
        private static readonly Brush sliderOutsideBrush = Brushes.White;
        private static readonly Brush comboTextBrush = Brushes.White;
        private static readonly Brush spinnerBrush = Brushes.White;
        private static readonly Brush followPointBrush = Brushes.White;

        private static readonly Dictionary<string, BitmapSource> cache = new();

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            // Value is a string for the pattern filename. The parameter is the OsuPatternFileHandler
            if (values.Length < 2 || values[0] is not string filename || values[1] is not OsuPatternFileHandler fileHandler) {
                return null;
            }

            // Check if the pattern is in the cache
            if (cache.TryGetValue(filename, out var cachedBitmap)) {
                return cachedBitmap;
            }

            var bitmapSource = DrawBeatmapFromFile(filename, fileHandler);
            cache.Add(filename, bitmapSource);
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
                using var font = new Font(FontFamily.GenericSansSerif, (float) (circleSize * 0.6), FontStyle.Bold);
                using var followPen = new Pen(followPointBrush, (float) circleSize * 0.1f) { DashStyle = DashStyle.Dash };

                // This is the next position because we are iterating in reverse time order
                HitObject nextHo = null;
                foreach (var hitObject in hitObjects) {
                    if (nextHo is null) {
                        nextHo = hitObject;
                        continue;
                    }

                    // Draw a follow line
                    if (!nextHo.ActualNewCombo && Vector2.Distance(nextHo.Pos, hitObject.EndPos) > circleSize * 2.5) {
                        var dist = Vector2.Distance(nextHo.Pos, hitObject.EndPos);
                        var p1 = Vector2.Lerp(nextHo.Pos, hitObject.EndPos, circleSize / dist * 1.2);
                        var p2 = Vector2.Lerp(nextHo.Pos, hitObject.EndPos, 1 - circleSize / dist * 1.2);
                        gfx.DrawLine(followPen, (float) p1.X, (float) p1.Y, (float) p2.X, (float) p2.Y);
                    }

                    DrawHitObject(gfx, nextHo, circleSize, font, sliderPaths);
                    nextHo = hitObject;
                }

                DrawHitObject(gfx, nextHo, circleSize, font, sliderPaths);

                return bmp.ToBitmapSource();
            } catch {
                return null;
            }
        }

        private void DrawHitObject(Graphics gfx, HitObject hitObject, double circleSize, Font font, Dictionary<HitObject, SliderPath> sliderPaths) {
            var pos = hitObject.StackedPos;
            if (hitObject.IsSlider) {
                if (!sliderPaths.ContainsKey(hitObject)) return;

                using var outlinePen = new Pen(sliderOutsideBrush, (float) circleSize * 1.95f) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round };
                using var insidePen = new Pen(sliderInsideBrush, (float) circleSize * 1.65f) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round };
                var path = sliderPaths[hitObject];

                using GraphicsPath gc = new GraphicsPath();
                var points = path.CalculatedPath.Select(p => new PointF((float) p.X, (float) p.Y)).ToArray();
                gc.AddLines(points);

                gfx.DrawPath(outlinePen, gc);
                gfx.DrawPath(insidePen, gc);
                //DrawCircleAtProgress(gfx, path, 1, circleSize);
                DrawCircleAtProgress(gfx, path, 0, circleSize);
                DrawTextAtPos(gfx, comboTextBrush, font, hitObject.ComboIndex.ToString(), pos);
            } else if (hitObject.IsSpinner) {
                using var pen = new Pen(spinnerBrush, (float) circleSize * PenWidth);
                DrawCircleAtPos(gfx, pen, new Vector2(256, 192), 150);
                DrawCircleAtPos(gfx, pen, new Vector2(256, 192), 5);
            } else {
                DrawFilledCircleAtPos(gfx, pos, circleSize);
                DrawTextAtPos(gfx, comboTextBrush, font, hitObject.ComboIndex.ToString(), pos);
            }
        }

        private void DrawCircleAtProgress(Graphics gfx, SliderPath path, double progress, double circleSize) {
            var pos = path.PositionAt(progress);
            DrawFilledCircleAtPos(gfx, pos, circleSize);
        }

        private void DrawFilledCircleAtPos(Graphics gfx, Vector2 pos, double radius) {
            DrawFilledCircleAtPos(gfx, circleOutsideBrush, pos, radius);
            DrawFilledCircleAtPos(gfx, circleInsideBrush, pos, radius * 0.846);
        }

        private void DrawFilledCircleAtPos(Graphics gfx, Brush brush, Vector2 pos, double radius) {
            var x = (int) (pos.X - radius);
            var y = (int) (pos.Y - radius);
            var s = (int) (radius * 2);
            gfx.FillEllipse(brush, x, y, s, s);
        }

        private void DrawCircleAtPos(Graphics gfx, Pen pen, Vector2 pos, double radius) {
            var x = (int) (pos.X - radius);
            var y = (int) (pos.Y - radius);
            var s = (int) (radius * 2);
            gfx.DrawEllipse(pen, x, y, s, s);
        }

        private void DrawTextAtPos(Graphics gfx, Brush brush, Font font, string text, Vector2 pos) {
            var textSize = gfx.MeasureString(text, font);
            gfx.DrawString(text, font, brush, (float) (pos.X - textSize.Width * 0.5), (float) (pos.Y - textSize.Height * 0.5));
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            throw new NotSupportedException("OsuPatternToThumbnailConverter is a OneWay converter.");
        }
    }
}