using System;
using System.Windows.Controls;

namespace Mapping_Tools.Components.TimeLine {
    /// <summary>
    /// Interaction logic for TimelineMark.xaml
    /// </summary>
    public partial class TimeLineMark :UserControl {
        public double Time { get; set; }

        public TimeLineMark(double m_seconds) {
            InitializeComponent();
            Time = m_seconds;

            TimeSpan ts = TimeSpan.FromMilliseconds(m_seconds);
            String m = ts.Minutes.ToString();
            if( m.Length < 2 )
                m = "0" + m;
            String s = ts.Seconds.ToString();
            if( s.Length < 2)
                s = "0" + s;
            this.text.Text = m + ":" + s;
        }
    }
}
