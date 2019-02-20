using System.Windows.Controls;

namespace Mapping_Tools.Components.TimeLine {
    /// <summary>
    /// Interaction logic for TimelineMark.xaml
    /// </summary>
    public partial class TimeLineMark :UserControl {
        public int Time { get; set; }

        public TimeLineMark(int milli_seconds) {
            InitializeComponent();

            Time = milli_seconds;
            string m = ( Time / 1000 / 60 ).ToString();
            if( m.Length < 2 )
                m = "0" + m;
            string s = ( (Time / 1000) % 60 ).ToString();
            if( s.Length < 2 )
                s = "0" + s;
            this.text.Text = m + ":" + s;
        }
    }
}
