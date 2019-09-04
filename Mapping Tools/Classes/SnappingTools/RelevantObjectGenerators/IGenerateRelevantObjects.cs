using Mapping_Tools.Classes.BeatmapHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.SnappingTools.RelevantObjectGenerators {
    public interface IGenerateRelevantObjects {
        bool IsActive { get; set; }
        string Name { get; }
        GeneratorType GeneratorType { get; }
    }
}
