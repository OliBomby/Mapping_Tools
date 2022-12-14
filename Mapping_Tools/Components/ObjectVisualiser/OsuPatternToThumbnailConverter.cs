using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Data;
using Mapping_Tools.Classes;
using Mapping_Tools.Classes.Tools.PatternGallery;
using Point = System.Drawing.Point;

namespace Mapping_Tools.Components.ObjectVisualiser {
    public class OsuPatternToThumbnailConverter : IMultiValueConverter {
        private const int thumbnailWidth = 150;
        private const int thumbnailHeight = 110;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            // Value is a string for the pattern filename. The parameter is the OsuPatternFileHandler
            if (values.Length < 2 || values[0] is not string filename || values[1] is not OsuPatternFileHandler fileHandler) {
                return null;
            }

            try {
                // Load the beatmap
                var beatmap = fileHandler.GetPatternBeatmap(filename);
            } catch {
                return null;
            }

            // Draw the thumbnail
            Random rand = new Random();
            using var bmp = new Bitmap(thumbnailWidth, thumbnailHeight);
            using var gfx = Graphics.FromImage(bmp);
            using var pen = new Pen(Color.White);

            // Draw one thousand random white lines on a dark blue background
            gfx.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            gfx.Clear(Color.Navy);
            for (int i = 0; i < 1000; i++)
            {
                var pt1 = new Point(rand.Next(bmp.Width), rand.Next(bmp.Height));
                var pt2 = new Point(rand.Next(bmp.Width), rand.Next(bmp.Height));
                gfx.DrawLine(pen, pt1, pt2);
            }

            return bmp.ToBitmapSource();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            throw new NotSupportedException("OsuPatternToThumbnailConverter is a OneWay converter.");
        }
    }
}