using System;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Mapping_Tools.Components.TimeLine {
    /// <summary>
    /// Interaction logic for TimelineElement.xaml
    /// </summary>
    public partial class TimeLineElement :UserControl {
        new TimeLine Parent;
        int Seconds;
        int Action;
        string Display = "";
        SolidColorBrush Inner;
        SolidColorBrush Outer;
        /// <summary>
        /// Creates the visual TimelineElement
        /// </summary>
        public TimeLineElement(TimeLine parent, int height, int seconds, int action) {
            InitializeComponent();
            Action = action;
            Seconds = seconds;
            Parent = parent;
            rectOuter.Height = height;
            rectInner.Height = height;

            SetupColour();

            SetupTooltip();
        }

        public TimeLineElement(TimeLine parent, int height, double seconds, int action) {
            InitializeComponent();

            Action = action;
            this.Seconds = (int) Math.Round(seconds / 1000);
            this.Parent = parent;
            rectOuter.Height = height;
            rectInner.Height = height;

            SetupColour();

            // Create friendly time for tooltip text
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
            string m = ( Seconds / 60 ).ToString();
            if( m.Length < 2 )
                m = "0" + m;
            string s = ( ( Seconds ) % 60 ).ToString();
            if( s.Length < 2 )
                s = "0" + s;
            Display = m + ":" + s;
            mainCanvas.ToolTip = Display;
        }
    }
}
