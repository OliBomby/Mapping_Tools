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
        bool IsSelected { get; set; }
        RelevantObjectLayer Layer { get; set; }
        RelevantObjectsGenerator Generator { get; set; }
        HashSet<IRelevantObject> ParentObjects { get; set; }
        HashSet<IRelevantObject> ChildObjects { get; set; }
        HashSet<IRelevantObject> GetParentage();
        void UpdateRelevancy();
        void UpdateTime();
        void UpdateSelected();
        void Consume(IRelevantObject other);
        double DistanceTo(IRelevantObject relevantObject);
    }
}