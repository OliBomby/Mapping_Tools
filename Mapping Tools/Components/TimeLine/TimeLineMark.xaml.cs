using System;
using System.Windows.Controls;

namespace Mapping_Tools.Components.TimeLine {
    /// <summary>
    /// Interaction logic for TimelineMark.xaml
    /// </summary>
    public partial class TimeLineMark :UserControl {
        public int Time { get; set; }

        public TimeLineMark(int seconds) {
            InitializeComponent();
            Time = seconds;
            string m = ( Time / 60 ).ToString();
            if( m.Length < 2 )
                m = "0" + m;
            string s = ( Time % 60 ).ToString();
            if( s.Length < 2 )
                s = "0" + s;
            this.text.Text = m + ":" + s;
        }
    }
}
