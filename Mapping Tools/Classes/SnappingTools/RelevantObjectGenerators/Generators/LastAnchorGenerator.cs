﻿using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.BeatmapHelper;

namespace Mapping_Tools.Classes.SnappingTools.RelevantObjectGenerators.Generators {
    class LastAnchorGenerator : RelevantObjectsGenerator, IGeneratePointsFromHitObjects {
        public override string Name => "Point on Last Anchor of Slider Generator";
        public override string Tooltip => "Generates virtual points on the last anchors of sliders.";
        public override GeneratorType GeneratorType => GeneratorType.Generators;

        public List<RelevantPoint> GetRelevantObjects(List<HitObject> objects) {
            return (from ho in objects where ho.IsSlider select new RelevantPoint(ho.CurvePoints.Last())).ToList();
        }
    }
}