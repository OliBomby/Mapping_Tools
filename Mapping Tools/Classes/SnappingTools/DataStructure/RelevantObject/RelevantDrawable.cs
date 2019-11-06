using System.Windows.Media;
using Mapping_Tools.Classes.MathUtil;
using MaterialDesignColors.ColorManipulation;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject {
    public abstract class RelevantDrawable : RelevantObject, IRelevantDrawable {
        public bool IsHighlighted { get; set; }

        public abstract double DistanceTo(Vector2 point);

        public abstract Vector2 NearestPoint(Vector2 point);

        public abstract bool Intersection(IRelevantObject other, out Vector2[] intersections);

        public abstract void DrawYourself(DrawingContext context, CoordinateConverter converter,
            SnappingToolsPreferences preferences);

        protected Pen GetPen(RelevantObjectPreferences preferences) {
            return new Pen() {
                Brush = new SolidColorBrush {
                    Color = AdjustColor(preferences.Color),
                    Opacity = AdjustOpacity(preferences.Opacity),
                },
                DashStyle = preferences.GetDashStyle(),
                Thickness = preferences.Thickness,
            };
        }

        protected Color AdjustColor(Color color) {
            var hsb = color.ToHsb();
            return new Hsb( IsSelected ? hsb.Hue + 100 : hsb.Hue, hsb.Saturation, hsb.Brightness).ToColor();
        }

        protected double AdjustOpacity(double opacity) {
            return opacity;
        }
    }
}
