using Mapping_Tools.classes.BeatmapHelper;
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
        int StartMilliSeconds;
        int EndMilliSeconds;
        int IntervalMilliSeconds;

        public TimeLine(int w, int h, int total_MilliSeconds) {
            InitializeComponent();

            TimeLineWidth = w - 40;
            TimeLineHeight = h;
            StartMilliSeconds = 0;
            EndMilliSeconds = total_MilliSeconds;
            IntervalMilliSeconds = (int) Math.Round(total_MilliSeconds / 20.0);
        }

        public static void AddTimingPoints(List<TimingPoint> tpoints, TimeLine tl) {
            foreach( TimingPoint tpoint_s in tpoints ) {
                tl.AddElement((int) tpoint_s.Offset);
            }
        }
        
        public static void RecalculateTimeLine(TimeLine tl) {
            tl.TimeLineWidth = (int) MainWindow.AppWindow.ActualWidth;
        }

        public void AddElement(int milli_seconds) {
            TimeLineElement te = new TimeLineElement(this, TimeLineInnerHeight, milli_seconds, false);
            TElements.Add(te);
            mainCanvas.Children.Add(te);
            Canvas.SetTop(te, ElementTop);
            Canvas.SetLeft(te, ( TimeLineWidth * 1000 * ( milli_seconds - StartMilliSeconds ) / ( EndMilliSeconds - StartMilliSeconds ) ) - 1);
        }

        public void GenerateMarkerElements() {
            foreach(TimeLineMark tMark_s in TMarks) {
                TimeLineElement te = new TimeLineElement(this, TimeLineInnerHeight, tMark_s.Time, true);
                TElements.Add(te);
                mainCanvas.Children.Add(te);
                Canvas.SetTop(te, ElementTop);
                Canvas.SetLeft(te,  ( TimeLineWidth * ( tMark_s.Time - StartMilliSeconds ) / ( EndMilliSeconds - StartMilliSeconds ) ) - 1);
            }
        }

        public void Setup() {
            // Create first mark
            TimeLineMark tmStart = new TimeLineMark(StartMilliSeconds);
            TMarks.Add(tmStart);
            mainCanvas.Children.Add(tmStart);

            // Create middle marks
            int intervalCount = ( ( EndMilliSeconds - StartMilliSeconds ) / IntervalMilliSeconds ) - 1;
            for( int i = 1; i <= intervalCount; i++ ) {
                TimeLineMark tm = new TimeLineMark(StartMilliSeconds + ( IntervalMilliSeconds * i ));
                TMarks.Add(tm);
                mainCanvas.Children.Add(tm);
            }

            // Create last mark
            TimeLineMark tmEnd = new TimeLineMark(EndMilliSeconds);
            TMarks.Add(tmEnd);
            mainCanvas.Children.Add(tmEnd);

            // Setup spacing
            Spacing = TimeLineWidth / TMarks.Count;
            for( int k = 0; k < TMarks.Count; k++ ) {
                Canvas.SetLeft(TMarks[k], (Spacing * k));
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
