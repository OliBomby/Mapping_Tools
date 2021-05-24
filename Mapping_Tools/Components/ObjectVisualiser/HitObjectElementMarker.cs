using System.Windows;
using System.Windows.Media;

namespace Mapping_Tools.Components.ObjectVisualiser {
    public class HitObjectElementMarker {
        public double Progress { get; set; }
        public double Size { get; set; }
        public Brush Brush { get; set; }

        public HitObjectElementMarker(double progress, double size, Brush brush) {
            Progress = progress;
            Size = size;
            Brush = brush;
        }
    }
}