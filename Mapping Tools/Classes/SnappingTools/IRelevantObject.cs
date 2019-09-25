using System;
using System.Collections.Generic;
using Mapping_Tools.Classes.MathUtil;
using System.Windows.Media;
using Mapping_Tools.Classes.SnappingTools.RelevantObjectGenerators;

namespace Mapping_Tools.Classes.SnappingTools {
    public interface IRelevantObject : IDisposable {
        double Time { get; set; }
        double Relevancy { get; set; }
        bool Disposed { get; set; }
        ObjectLayer Layer { get; set; }
        RelevantObjectsGenerator Generator { get; set; }
        List<IRelevantObject> ParentObjects { get; set; }
        List<IRelevantObject> ChildObjects { get; set; }
        void UpdateRelevancy();
        void UpdateTime();
    }
}