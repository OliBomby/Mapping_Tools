using Mapping_Tools.Classes.TimingStudio;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Viewmodels
{
    public class TimingStudioVM
    {
        private string baseBeatmap { get; set; }
        private ObservableCollection<StudioTimingPoint> timingPoints { get; set; }

    }
}
