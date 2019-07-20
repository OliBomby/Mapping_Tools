using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Mapping_Tools.Components.TimeLine {
    /// <summary>
    /// Interaction logic for Timeline.xaml
    /// </summary>
    public partial class TimeLine :UserControl {
        readonly List<TimeLineMark> TMarks = new List<TimeLineMark>();
        readonly List<TimeLineElement> TElements = new List<TimeLineElement>();
        double TimeLineWidth;
        readonly double TimeLineHeight;
        double TimeLineInnerHeight;
        double ElementTop;
        double Spacing;
        readonly double StartMSeconds;
        readonly double EndMSeconds;
        readonly double IntervalMSeconds;

        public TimeLine(double w, double h, double m_seconds) {
            InitializeComponent();

            TimeLineWidth = w - 110;
            TimeLineHeight = h;
            StartMSeconds = 0;
            EndMSeconds = Math.Max(m_seconds, 20);
            IntervalMSeconds = EndMSeconds / 10.0;

            Setup();
        }

        public static void RecalculateTimeLine(TimeLine tl) {
            tl.TimeLineWidth = MainWindow.AppWindow.ActualWidth;
        }

        public void AddElement(double m_seconds, double action) {
            TimeLineElement te = new TimeLineElement(TimeLineInnerHeight, m_seconds, action);
            TElements.Add(te);
            mainCanvas.Children.Add(te);

            Canvas.SetTop(te, ElementTop);
            Canvas.SetLeft(te, ( TimeLineWidth * ( m_seconds - StartMSeconds ) / ( EndMSeconds - StartMSeconds ) ) - 1);
        }

        private void GenerateMarkerElements() {
            foreach( TimeLineMark tMark_s in TMarks ) {
                TimeLineElement te = new TimeLineElement(TimeLineInnerHeight, tMark_s.Time, 0);
                TElements.Add(te);
                mainCanvas.Children.Add(te);
                Canvas.SetTop(te, ElementTop);
                Canvas.SetLeft(te, ( TimeLineWidth * ( tMark_s.Time - StartMSeconds ) / ( EndMSeconds - StartMSeconds ) ) - 1);
            }
        }

        private void Setup() {
            // Create first mark
            TimeLineMark tmStart = new TimeLineMark(StartMSeconds);
            TMarks.Add(tmStart);
            mainCanvas.Children.Add(tmStart);

            // Create middle marks
            double intervalCount = ( ( EndMSeconds - StartMSeconds ) / IntervalMSeconds ) - 1;
            for( int i = 1; i <= intervalCount; i++ ) {
                TimeLineMark tm = new TimeLineMark(StartMSeconds + ( IntervalMSeconds * i ));
                TMarks.Add(tm);
                mainCanvas.Children.Add(tm);
            }

            // Create last mark
            TimeLineMark tmEnd = new TimeLineMark(EndMSeconds);
            TMarks.Add(tmEnd);
            mainCanvas.Children.Add(tmEnd);

            // Setup spacing
            Spacing = TimeLineWidth / ( TMarks.Count - 1 );
            for( int k = 0; k < TMarks.Count; k++ ) {
                Canvas.SetLeft(TMarks[k], ( Spacing * k ));
                Canvas.SetTop(TMarks[k], 1);
            }

            // Size & place the controls
            Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Arrange(new Rect(0, 0, TimeLineWidth, TimeLineHeight));

            //Create TimeLine Line
            Line line = new Line {
                X1 = 0,
                Y1 = TimeLineHeight / 2,
                X2 = TimeLineWidth,
                Y2 = TimeLineHeight / 2
            };

            // Create a Brush  
            SolidColorBrush blackBrush = new SolidColorBrush {
                Color = Color.FromRgb(214, 214, 214)
            };
            // Set Line's width and color  
            line.StrokeThickness = 2;
            line.Stroke = blackBrush;
            mainCanvas.Children.Add(line);

            // Canvas.Top value for TimelineElements
            ElementTop = 1 + (int) tmStart.ActualHeight + 1;
            // Height of region inside the timeline
            TimeLineInnerHeight = TimeLineHeight - 46 - 2;
            // Set the canvas's width
            //mainCanvas.Width = ( spacing * ( TMarks.Count - 1 ) ) + (int) tmEnd.ActualWidth;
            mainCanvas.Width = TimeLineWidth;
            mainCanvas.Height = 50;
            GenerateMarkerElements();
        }
    }
}
