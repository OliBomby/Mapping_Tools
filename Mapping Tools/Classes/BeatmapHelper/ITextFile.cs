using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.BeatmapHelper
{
    public interface ITextFile
    {
        List<string> GetLines();

        void SetLines(List<string> lines);
    }
}
