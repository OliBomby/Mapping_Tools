using System.Collections.Generic;
using Mapping_Tools.Classes.BeatmapHelper;

namespace Mapping_Tools.Classes.SnappingTools.RelevantObjectGenerators.Generators {
    class StartPointGenerator : RelevantObjectsGenerator, IGenerateRelevantObjectsFromHitObjects {
        public new string Name => "StartPoint Generator";
        public new GeneratorType GeneratorType => GeneratorType.Basic;

        public List<IRelevantObject> GetRelevantObjects(List<HitObject> objects) {
            List<IRelevantObject> newObjects = new List<IRelevantObject>();

            foreach (HitObject ho in objects) {
                if (ho.IsSpinner || ho.IsHoldNote)
                    continue;

                newObjects.Add(new RelevantPoint(ho.Pos));
            }

            return newObjects;
        }
    }
}
