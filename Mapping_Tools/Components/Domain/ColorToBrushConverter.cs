using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Core.Classes.BeatmapHelper;

namespace Mapping_Tools.Components.Domain
{
    /// <summary>
    /// Changes the <see cref="Color"/> to <see cref="SolidColorBrush"/> and back.
    /// </summary>
    public class ColorToBrushConverter : IValueConverter
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Color color)
            {
                return new SolidColorBrush(color);
            }
            if (value is RgbaColour rgba)
            {
                return new SolidColorBrush(Color.FromArgb(rgba.A, rgba.R, rgba.G, rgba.B));
            }
            return Binding.DoNothing;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
            {
                return brush.Color;
            }
            return default(Color);
        }
    }
}
