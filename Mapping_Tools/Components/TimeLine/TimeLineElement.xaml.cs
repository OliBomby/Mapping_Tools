using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Mapping_Tools.Components.TimeLine;

/// <summary>
/// Interaction logic for TimelineElement.xaml
/// </summary>
public partial class TimeLineElement :UserControl {
    readonly double milliseconds;
    readonly double action;
    SolidColorBrush inner;
    SolidColorBrush outer;
    /// <summary>
    /// Creates the visual TimelineElement
    /// </summary>
    public TimeLineElement(double height, double mSeconds, double action) {
        InitializeComponent();
        this.action = action;
        milliseconds = mSeconds;
        RectOuter.Height = height;
        RectInner.Height = height;

        SetupColour();

        SetupTooltip();
    }

    private void SetupColour() {
        switch( action ) {
            case 1:
                // Create a Brush  
                inner = new SolidColorBrush {
                    Color = Color.FromRgb(23, 180, 30)
                };

                // Create a Brush  
                outer = new SolidColorBrush {
                    Color = Color.FromRgb(88, 245, 15)
                };
                break;
            case 2:
                // Create a Brush  
                inner = new SolidColorBrush {
                    Color = Color.FromRgb(245, 173, 30)
                };

                // Create a Brush  
                outer = new SolidColorBrush {
                    Color = Color.FromRgb(249, 249, 6)
                };
                break;
            case 3:
                // Create a Brush  
                inner = new SolidColorBrush {
                    Color = Color.FromRgb(245, 77, 35)
                };

                // Create a Brush  
                outer = new SolidColorBrush {
                    Color = Color.FromRgb(252, 66, 20)
                };
                break;
            case 4:
                // Create a Brush  
                inner = new SolidColorBrush {
                    Color = Color.FromRgb(145, 37, 175)
                };

                // Create a Brush  
                outer = new SolidColorBrush {
                    Color = Color.FromRgb(172, 56, 190)
                };
                break;
            default:
                // Create a Brush  
                inner = new SolidColorBrush {
                    Color = Color.FromRgb(200, 200, 200)
                };

                // Create a Brush  
                outer = new SolidColorBrush {
                    Color = Color.FromRgb(120, 120, 120)
                };
                break;
        }
        RectInner.Fill = inner;
        RectOuter.Fill = outer;
    }

    // Creates tooltip from seconds value
    private void SetupTooltip() {
        TimeSpan ts = TimeSpan.FromMilliseconds(milliseconds);

        string m = ts.Minutes.ToString();
        if (m.Length < 2)
            m = "0" + m;
        string s = ts.Seconds.ToString();
        if (s.Length < 2)
            s = "0" + s;
        string ms = ts.Milliseconds.ToString();
        if (ms.Length < 2)
            ms = "0" + ms;

        MainCanvas.ToolTip = string.Format("{0}:{1}:{2}", m, s, ms);
    }

    //Open osu at timestamp
    private void OpenLink(object sender, RoutedEventArgs e) {
        System.Diagnostics.Process.Start("explorer.exe", "osu://edit/" + Math.Round(milliseconds));
    }
}