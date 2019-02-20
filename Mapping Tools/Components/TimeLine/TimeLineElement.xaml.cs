using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Mapping_Tools.Components.TimeLine {
    /// <summary>
    /// Interaction logic for TimelineElement.xaml
    /// </summary>
    public partial class TimeLineElement :UserControl {
        TimeLine Parent;
        int MilliSeconds;
        string Display = "";
        bool IsMarker;
        SolidColorBrush Inner;
        SolidColorBrush Outer;
        /// <summary>
        /// Creates the visual TimelineElement
        /// </summary>
        public TimeLineElement(TimeLine parent, int height, int milli_seconds, bool isMarker) {
            InitializeComponent();

            this.MilliSeconds = milli_seconds;
            this.Parent = parent;
            this.IsMarker = isMarker;
            rectOuter.Height = height;
            rectInner.Height = height;

            SetupColour();

            // Create friendly time for tooltip text
            SetupTooltip();
        }

        private void SetupColour() {
            if( IsMarker ) {
                // Create a Brush  
                Inner = new SolidColorBrush {
                    Color = Color.FromRgb(200, 200, 200)
                };

                // Create a Brush  
                Outer = new SolidColorBrush {
                    Color = Color.FromRgb(120, 120, 120)
                };
            }
            else {
                // Create a Brush  
                Inner = new SolidColorBrush {
                    Color = Color.FromRgb(23, 180, 30)
                };

                // Create a Brush  
                Outer = new SolidColorBrush {
                    Color = Color.FromRgb(88, 245, 15)
                };
            }

            rectInner.Fill = Inner;
            rectOuter.Fill = Outer;
        }
        // Creates tooltip from seconds value
        private void SetupTooltip() {
            string m = ( MilliSeconds / 1000 / 60 ).ToString();
            if( m.Length < 2 )
                m = "0" + m;
            string s = ( (MilliSeconds / 1000) % 60 ).ToString();
            if( s.Length < 2 )
                s = "0" + s;
            Display = m + ":" + s;
            mainCanvas.ToolTip = Display;
        }
    }
}
