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
        List<TimeLineMark> TMarks = new List<TimeLineMark>();
        List<TimeLineElement> TElements = new List<TimeLineElement>();
        int TimeLineWidth;
        int TimeLineHeight;
        int TimeLineInnerHeight;
        int ElementTop;
        int Spacing;
        int StartSeconds;
        int EndSeconds;
        int IntervalSeconds;

        public TimeLine(int w, int h, int seconds) {
            InitializeComponent();

            TimeLineWidth = w - 100;
            TimeLineHeight = h;
            StartSeconds = 0;
            EndSeconds = seconds;
            IntervalSeconds = (int) Math.Round(seconds / 20.0);

            Setup();
        }

        public TimeLine(int w, int h, double seconds) {
            InitializeComponent();

            TimeLineWidth = w - 60;
            TimeLineHeight = h;
            StartSeconds = 0;
            EndSeconds = (int) seconds / 1000;
            IntervalSeconds = (int) Math.Round(EndSeconds / 20.0);

            Setup();
        }

        public static void RecalculateTimeLine(TimeLine tl) {
            tl.TimeLineWidth = (int) MainWindow.AppWindow.ActualWidth;
        }

        public void AddElement(int seconds, int action) {
            TimeLineElement te = new TimeLineElement(this, TimeLineInnerHeight, seconds, action);
            TElements.Add(te);
            mainCanvas.Children.Add(te);

            Canvas.SetTop(te, ElementTop);
            Canvas.SetLeft(te, ( TimeLineWidth * ( seconds - StartSeconds ) / ( EndSeconds - StartSeconds ) ) - 1);
        }

        public void AddElement(double d_seconds, int action) {
            TimeLineElement te = new TimeLineElement(this, TimeLineInnerHeight, d_seconds, action);
            TElements.Add(te);
            mainCanvas.Children.Add(te);

            int seconds = (int) d_seconds / 1000;
            Canvas.SetTop(te, ElementTop);
            Canvas.SetLeft(te, ( TimeLineWidth * ( seconds - StartSeconds ) / ( EndSeconds - StartSeconds ) ) - 1);
        }

        private void GenerateMarkerElements() {
            foreach( TimeLineMark tMark_s in TMarks ) {
                TimeLineElement te = new TimeLineElement(this, TimeLineInnerHeight, tMark_s.Time, 0);
                TElements.Add(te);
                mainCanvas.Children.Add(te);
                Canvas.SetTop(te, ElementTop);
                Canvas.SetLeft(te, ( TimeLineWidth * ( tMark_s.Time - StartSeconds ) / ( EndSeconds - StartSeconds ) ) - 1);
            }
        }

        private void Setup() {
            // Create first mark
            TimeLineMark tmStart = new TimeLineMark(StartSeconds);
            TMarks.Add(tmStart);
            mainCanvas.Children.Add(tmStart);

            // Create middle marks
            int intervalCount = ( ( EndSeconds - StartSeconds ) / IntervalSeconds ) - 1;
            for( int i = 1; i <= intervalCount; i++ ) {
                TimeLineMark tm = new TimeLineMark(StartSeconds + ( IntervalSeconds * i ));
                TMarks.Add(tm);
                mainCanvas.Children.Add(tm);
            }

            // Create last mark
            TimeLineMark tmEnd = new TimeLineMark(EndSeconds);
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
