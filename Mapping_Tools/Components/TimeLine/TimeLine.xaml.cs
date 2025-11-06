using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Mapping_Tools.Components.TimeLine;

/// <summary>
/// Interaction logic for Timeline.xaml
/// </summary>
public partial class TimeLine :UserControl {
    readonly List<TimeLineMark> marks = new List<TimeLineMark>();
    readonly List<TimeLineElement> elements = new List<TimeLineElement>();
    double timeLineWidth;
    readonly double timeLineHeight;
    double timeLineInnerHeight;
    double elementTop;
    double spacing;
    readonly double startMSeconds;
    readonly double endMSeconds;
    readonly double intervalMSeconds;

    public TimeLine(double w, double h, double mSeconds) {
        InitializeComponent();

        timeLineWidth = w - 110;
        timeLineHeight = h;
        startMSeconds = 0;
        endMSeconds = Math.Max(mSeconds, 20);
        intervalMSeconds = endMSeconds / 10.0;

        Setup();
    }

    public static void RecalculateTimeLine(TimeLine tl) {
        tl.timeLineWidth = MainWindow.AppWindow.ActualWidth;
    }

    public void AddElement(double mSeconds, double action) {
        TimeLineElement te = new TimeLineElement(timeLineInnerHeight, mSeconds, action);
        elements.Add(te);
        MainCanvas.Children.Add(te);

        Canvas.SetTop(te, elementTop);
        Canvas.SetLeft(te, ( timeLineWidth * ( mSeconds - startMSeconds ) / ( endMSeconds - startMSeconds ) ) - 1);
    }

    private void GenerateMarkerElements() {
        foreach( TimeLineMark tMark_s in marks ) {
            TimeLineElement te = new TimeLineElement(timeLineInnerHeight, tMark_s.Time, 0);
            elements.Add(te);
            MainCanvas.Children.Add(te);
            Canvas.SetTop(te, elementTop);
            Canvas.SetLeft(te, ( timeLineWidth * ( tMark_s.Time - startMSeconds ) / ( endMSeconds - startMSeconds ) ) - 1);
        }
    }

    private void Setup() {
        // Create first mark
        TimeLineMark tmStart = new TimeLineMark(startMSeconds);
        marks.Add(tmStart);
        MainCanvas.Children.Add(tmStart);

        // Create middle marks
        double intervalCount = ( ( endMSeconds - startMSeconds ) / intervalMSeconds ) - 1;
        for( int i = 1; i <= intervalCount; i++ ) {
            TimeLineMark tm = new TimeLineMark(startMSeconds + ( intervalMSeconds * i ));
            marks.Add(tm);
            MainCanvas.Children.Add(tm);
        }

        // Create last mark
        TimeLineMark tmEnd = new TimeLineMark(endMSeconds);
        marks.Add(tmEnd);
        MainCanvas.Children.Add(tmEnd);

        // Setup spacing
        spacing = timeLineWidth / ( marks.Count - 1 );
        for( int k = 0; k < marks.Count; k++ ) {
            Canvas.SetLeft(marks[k], ( spacing * k ));
            Canvas.SetTop(marks[k], 1);
        }

        // Size & place the controls
        Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        Arrange(new Rect(0, 0, timeLineWidth, timeLineHeight));

        //Create TimeLine Line
        Line line = new Line {
            X1 = 0,
            Y1 = timeLineHeight / 2,
            X2 = timeLineWidth,
            Y2 = timeLineHeight / 2
        };

        // Create a Brush  
        SolidColorBrush blackBrush = new SolidColorBrush {
            Color = Color.FromRgb(214, 214, 214)
        };
        // Set Line's width and color  
        line.StrokeThickness = 2;
        line.Stroke = blackBrush;
        MainCanvas.Children.Add(line);

        // Canvas.Top value for TimelineElements
        elementTop = 1 + (int) tmStart.ActualHeight + 1;
        // Height of region inside the timeline
        timeLineInnerHeight = timeLineHeight - 46 - 2;
        // Set the canvas's width
        //mainCanvas.Width = ( spacing * ( TMarks.Count - 1 ) ) + (int) tmEnd.ActualWidth;
        MainCanvas.Width = timeLineWidth;
        MainCanvas.Height = 50;
        GenerateMarkerElements();
    }
}