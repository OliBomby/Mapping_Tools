using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Mapping_Tools.Components.TimeLine {
    /// <summary>
    /// Interaction logic for TimelineElement.xaml
    /// </summary>
    public partial class TimeLineElement :UserControl {
        new TimeLine Parent;
        double Milliseconds;
        double Action;
        SolidColorBrush Inner;
        SolidColorBrush Outer;
        /// <summary>
        /// Creates the visual TimelineElement
        /// </summary>
        public TimeLineElement(TimeLine parent, double height, double m_seconds, double action) {
            InitializeComponent();
            Action = action;
            Milliseconds = m_seconds;
            Parent = parent;
            rectOuter.Height = height;
            rectInner.Height = height;

            SetupColour();

            SetupTooltip();
        }

        private void SetupColour() {
            switch( Action ) {
                case 1:
                    // Create a Brush  
                    Inner = new SolidColorBrush {
                        Color = Color.FromRgb(23, 180, 30)
                    };

                    // Create a Brush  
                    Outer = new SolidColorBrush {
                        Color = Color.FromRgb(88, 245, 15)
                    };
                    break;
                case 2:
                    // Create a Brush  
                    Inner = new SolidColorBrush {
                        Color = Color.FromRgb(245, 173, 30)
                    };

                    // Create a Brush  
                    Outer = new SolidColorBrush {
                        Color = Color.FromRgb(249, 249, 6)
                    };
                    break;
                case 3:
                    // Create a Brush  
                    Inner = new SolidColorBrush {
                        Color = Color.FromRgb(245, 77, 35)
                    };

                    // Create a Brush  
                    Outer = new SolidColorBrush {
                        Color = Color.FromRgb(252, 66, 20)
                    };
                    break;
                default:
                    // Create a Brush  
                    Inner = new SolidColorBrush {
                        Color = Color.FromRgb(200, 200, 200)
                    };

                    // Create a Brush  
                    Outer = new SolidColorBrush {
                        Color = Color.FromRgb(120, 120, 120)
                    };
                    break;
            }
            rectInner.Fill = Inner;
            rectOuter.Fill = Outer;
        }

        // Creates tooltip from seconds value
        private void SetupTooltip() {
            TimeSpan ts = TimeSpan.FromMilliseconds(Milliseconds);

            String m = ts.Minutes.ToString();
            if (m.Length < 2)
                m = "0" + m;
            String s = ts.Seconds.ToString();
            if (s.Length < 2)
                s = "0" + s;
            String ms = ts.Milliseconds.ToString();
            if (ms.Length < 2)
                ms = "0" + ms;

            mainCanvas.ToolTip = String.Format("{0}:{1}:{2}", m, s, ms);
        }

        //Open osu at timestamp
        private void OpenLink(object sender, RoutedEventArgs e) {
            ProcessStartInfo ProcessInfo;
            Process Process;

            ProcessInfo = new ProcessStartInfo("cmd.exe", "/K " + "start osu://edit/" + Math.Round(Milliseconds)) {
                CreateNoWindow = true,
                UseShellExecute = false
            };

            Process = Process.Start(ProcessInfo);
        }
    }
}
