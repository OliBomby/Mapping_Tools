using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject.RelevantObjects;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Generators;

public class AnchorPointGenerator : RelevantObjectsGenerator {
    public override string Name => "Points on Slider Anchors";
    public override string Tooltip => "Generates virtual points on the anchor points of sliders.";
    public override GeneratorType GeneratorType => GeneratorType.Basic;
    public override GeneratorTemporalPositioning TemporalPositioning => GeneratorTemporalPositioning.Custom;

    public AnchorPointGenerator() {
        Settings.RelevancyRatio = 0.8;
        Settings.IsActive = true;
    }

    [RelevantObjectsGeneratorMethod]
    public IEnumerable<RelevantPoint> GetRelevantObjects(RelevantHitObject relevantHitObject) {
        var ho = relevantHitObject.HitObject;
        if (!ho.IsSlider) return null;
        var curvePoints = ho.GetAllCurvePoints();
        return curvePoints.Select((o, i) => new RelevantPoint(o) { CustomTime = (double)i / (curvePoints.Count - 1) * (ho.EndTime - ho.Time) + ho.Time });
    }
}