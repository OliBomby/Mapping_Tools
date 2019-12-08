using Mapping_Tools.Classes.BeatmapHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.ExternalFileUtil.Reaper
{
    public class ReaperEditor : Editor
    {
        public ReaperProject project => (ReaperProject)TextFile;

        public ReaperEditor(List<string> lines) 
            => TextFile = new ReaperProject(lines);
        
        public ReaperEditor(string path)
        {
            Path = path;
            TextFile = new ReaperProject(path);
        }

    }
}
