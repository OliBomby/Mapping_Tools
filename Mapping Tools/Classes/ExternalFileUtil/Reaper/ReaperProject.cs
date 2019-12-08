using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.ExternalFileUtil.Reaper
{
    public class ReaperProject
    {
        private ObservableCollection<EnvelopeTempoPoint> _masterTempoPoints;

        /// <summary>
        /// The master list of <see cref="EnvelopeTempoPoint"/>s
        /// </summary>
        public ObservableCollection<EnvelopeTempoPoint> MasterTempoPoints
        {
            get { return _masterTempoPoints; }
            set { _masterTempoPoints = value; }
        }

        private ObservableCollection<TrackItem> _trackItems;

        public ObservableCollection<TrackItem> TrackItems
        {
            get { return _trackItems; }
            set { _trackItems = value; }
        }







    }
}
