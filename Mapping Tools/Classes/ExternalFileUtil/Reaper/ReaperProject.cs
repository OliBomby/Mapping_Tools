using Mapping_Tools.Classes.BeatmapHelper;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.ExternalFileUtil.Reaper
{
    public class ReaperProject : ITextFile
    {
        public List<decimal> ProjectOffsets { get; set; }

        public List<int> MaxProjectLength { get; set; }

        public List<decimal> Grid { get; set; }


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


        private ObservableCollection<TrackItem> _trackItem;

        public ReaperProject(List<string> lines)
        {

        }

        public ReaperProject(string path)
        {
        }

        public ObservableCollection<TrackItem> TrackItem
        {
            get { return _trackItem; }
            set { _trackItem = value; }
        }

        public List<string> GetLines()
        {
            throw new NotImplementedException();
        }

        public void SetLines(List<string> lines)
        {
            _masterTempoPoints = GetCategoryChunk(lines, "TEMPOENVEX");

        }

        private ObservableCollection<EnvelopeTempoPoint> GetCategoryChunk(List<String> lines, string category, string[] categoryIdentifiers = null)
        {
            if (categoryIdentifiers == null)
                categoryIdentifiers = new[] { "<" , ">"};

            ObservableCollection<EnvelopeTempoPoint> categoryLines = new ObservableCollection<EnvelopeTempoPoint>();
            bool atCategory = false;

            foreach (string line in lines)
            {
                if (atCategory && line != "")
                {
                    if (categoryIdentifiers.Any(o => line.StartsWith(o))) // Reached another category
                    {
                        break;
                    }
                    
                    if (line.StartsWith("    PT"))
                        categoryLines.Add(new EnvelopeTempoPoint(line));
                }
                else
                {
                    if (line == category)
                    {
                        atCategory = true;
                    }
                }
            }
            return categoryLines;
        }
    }
 }

