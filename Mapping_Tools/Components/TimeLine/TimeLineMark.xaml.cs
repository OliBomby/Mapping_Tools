﻿using System;
using System.Windows.Controls;

namespace Mapping_Tools.Components.TimeLine {
    /// <summary>
    /// Interaction logic for TimelineMark.xaml
    /// </summary>
    public partial class TimeLineMark :UserControl {
        public double Time { get; set; }

        public TimeLineMark(double mSeconds) {
            InitializeComponent();
            Time = mSeconds;

            TimeSpan ts = TimeSpan.FromMilliseconds(mSeconds);
            string m = ts.Minutes.ToString();
            if( m.Length < 2 )
                m = "0" + m;
            string s = ts.Seconds.ToString();
            if( s.Length < 2)
                s = "0" + s;

            Text.Text = string.Format("{0}:{1}", m, s);
        }
    }
}
