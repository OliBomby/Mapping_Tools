using Mapping_Tools.Components.TimeLine;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Mapping_Tools.Components.TimingStudio
{
    /// <summary>
    /// Interaction logic for TimingStudioTimeline.xaml
    /// </summary>
    public partial class TimingStudioTimeline : INotifyPropertyChanged
    {
        /// <summary>
        /// The spectrum that is shown behind the timeline.
        /// </summary>
        //private Spectrum.Spectrum TimelineSpectrum = new Spectrum.Spectrum();

        private List<StudioTimingPointGraphic> timingPoints = new List<StudioTimingPointGraphic>();

        /// <summary>
        /// The total width of the Timeline in pixels.
        /// </summary>
        private double TimelineWidth;

        /// <summary>
        /// The total height of the Timeline in pixels.
        /// </summary>
        private double TimelineHeight;

        /// <summary>
        /// The height of the region inside the timeline.
        /// </summary>
        private double TimelineInnerHeight;

        /// <summary>
        /// The <see cref="Canvas"/>.Top value for the Timeline Elements.
        /// </summary>
        private double ElementTop;

        /// <summary>
        /// The spacing of the canvas positions to the window.
        /// </summary>
        private double Spacing;

        /// <summary>
        /// The starting audio time in miliseconds.
        /// </summary>
        private double StartTime;

        /// <summary>
        /// The ending audio time in miliseconds.
        /// </summary>
        private double EndTime;

        /// <summary>
        /// The entire length of the audio time selection divided by 10.
        /// </summary>
        private double IntervalTime;


        public TimingStudioTimeline()
        {
            InitializeComponent();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void ThisMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {

        }
    }
}
