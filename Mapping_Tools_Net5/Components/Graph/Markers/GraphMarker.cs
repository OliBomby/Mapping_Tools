using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Mapping_Tools.Components.Graph.Markers {
    public class GraphMarker : UIElement {
        private double MarkerLengthExtra => DrawMarker ? MarkerLength : 0;
        private Pen Pen => new Pen(CustomLineBrush ?? Stroke, 1.0);

        public Brush Stroke { get; set; }

        public Brush CustomLineBrush { get; set; }

        public double MarkerLength { get; set; }

        public double Width { get; set; }
        public double Height { get; set; }

        public double X { get; set; }
        public double Y { get; set; }

        public Orientation Orientation { get; set; }

        public string Text { get; set; }

        public double Value { get; set; }

        public Color MarkerColor { get; set; }

        public bool DrawMarker { get; set; }

        public bool Visible { get; set; } = true;

        public bool Snappable { get; set; } = false;
 
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (!Visible) {
                return;
            }

            var ft = Text != null ? new FormattedText(Text, CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight, new Typeface("Segoe UI"), 16, Stroke,
                VisualTreeHelper.GetDpi(this).PixelsPerDip) : null;

            if (Orientation == Orientation.Horizontal) {
                drawingContext.DrawLine(Pen, new Point(X, Y), new Point(X + Width, Y));

                // This draws the little extension beyond the bounds of the graph
                if (DrawMarker) {
                    drawingContext.DrawLine(new Pen(new SolidColorBrush(MarkerColor), 1.0),
                        new Point(X - MarkerLength, Y), new Point(X, Y));
                }

                if (ft != null)
                    drawingContext.DrawText(ft, new Point(X - 5 - ft.Width - MarkerLengthExtra, Y - ft.Height / 2));
            } else {
                drawingContext.DrawLine(Pen, new Point(X, Y), new Point(X, Y + Height));
                
                // This draws the little extension beyond the bounds of the graph
                if (DrawMarker) {
                    drawingContext.DrawLine(new Pen(new SolidColorBrush(MarkerColor), 1.0),
                        new Point(X, Y + Height), new Point(X, Y + Height + MarkerLength));
                }

                if (ft != null)
                    drawingContext.DrawText(ft, new Point(X - ft.Width / 2, Y + Height + 5 + MarkerLengthExtra));
            }
        }
    }
}