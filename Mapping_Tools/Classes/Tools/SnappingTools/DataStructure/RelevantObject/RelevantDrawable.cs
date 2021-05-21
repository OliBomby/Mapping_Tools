using System.Windows.Media;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.Tools.SnappingTools.Serialization;
using MaterialDesignColors.ColorManipulation;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject {
    public abstract class RelevantDrawable : RelevantObject, IRelevantDrawable {
        public abstract string PreferencesName { get; }

        public abstract double DistanceTo(Vector2 point);

        public abstract Vector2 NearestPoint(Vector2 point);

        public abstract bool Intersection(IRelevantObject other, out Vector2[] intersections);

        public virtual void DrawYourself(DrawingContext context, CoordinateConverter converter, SnappingToolsPreferences preferences) {
            var relevantObjectPreferences = GetRelevantObjectPreferences(preferences);
            if (IsSelected) {
                DrawYourself(context, converter, relevantObjectPreferences, GetPenSelected(relevantObjectPreferences));
            }
            DrawYourself(context, converter, relevantObjectPreferences, GetPen(relevantObjectPreferences));
        }

        public abstract void DrawYourself(DrawingContext context, CoordinateConverter converter, RelevantObjectPreferences preferences, Pen pen);

        protected RelevantObjectPreferences GetRelevantObjectPreferences(SnappingToolsPreferences preferences) {
            return preferences.GetReleventObjectPreferences(PreferencesName);
        }

        protected Pen GetPen(RelevantObjectPreferences preferences) {
            return new Pen {
                Brush = new SolidColorBrush {
                    Color = AdjustColor(preferences.Color),
                    Opacity = AdjustOpacity(preferences.Opacity)
                },
                DashStyle = preferences.GetDashStyle(),
                Thickness = preferences.Thickness
            };
        }

        protected Pen GetPenSelected(RelevantObjectPreferences preferences) {
            return new Pen {
                Brush = new SolidColorBrush {
                    Color = Color.FromArgb(255, 255, 200, 0),
                    Opacity = 1
                },
                DashStyle = DashStyles.Solid,
                Thickness = preferences.Thickness + 2
            };
        }

        private Color AdjustColor(Color color) {
            var hsb = color.ToHsb();
            return new Hsb( hsb.Hue,
                IsLocked ? IsSelected ? hsb.Saturation * 0.6 : hsb.Saturation * 0.3 : hsb.Saturation,
                IsInheritable ? hsb.Brightness : hsb.Brightness * 0.5).ToColor();
        }

        private double AdjustOpacity(double opacity) {
            return Relevancy * opacity;
        }
    }
}
