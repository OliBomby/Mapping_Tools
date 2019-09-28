using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;
using System;
using System.Collections.Generic;
using Mapping_Tools.Classes.SnappingTools.DataStructure.Layers;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject {
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