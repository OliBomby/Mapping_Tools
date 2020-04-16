using Mapping_Tools.Classes.BeatmapHelper;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Mapping_Tools.Classes.ExternalFileUtil.Reaper {
    public class ReaperProject : ITextFile
    {
        public List<decimal> ProjectOffsets { get; set; }

        public List<int> MaxProjectLength { get; set; }

        public List<decimal> Grid { get; set; }


        /// <summary>
        /// The master list of <see cref="EnvelopeTempoPoint"/>s
        /// </summary>
        public ObservableCollection<EnvelopeTempoPoint> MasterTempoPoints { get; set; }

        public ObservableCollection<TrackItem> TrackItems { get; set; }


        public ReaperProject(List<string> lines)
        {

        }

        public ReaperProject(string path)
        {
        }

        public ObservableCollection<TrackItem> TrackItem { get; set; }

        public List<string> GetLines()
        {
            throw new NotImplementedException();
        }

        public void SetLines(List<string> lines)
        {
            MasterTempoPoints = GetCategoryChunk(lines, "TEMPOENVEX");

        }

        private ObservableCollection<EnvelopeTempoPoint> GetCategoryChunk(IEnumerable<string> lines, string category, string[] categoryIdentifiers = null)
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

