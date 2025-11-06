using System.Linq;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject.RelevantObjects;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Generators;

public class LastAnchorGenerator : RelevantObjectsGenerator {
    public override string Name => "Points on Last Anchors";
    public override string Tooltip => "Generates virtual points on the last anchors of sliders.";
    public override GeneratorType GeneratorType => GeneratorType.Basic;
    public override GeneratorTemporalPositioning TemporalPositioning => GeneratorTemporalPositioning.Custom;

    public LastAnchorGenerator() {
        Settings.RelevancyRatio = 1;
        Settings.IsActive = true;
    }

    [RelevantObjectsGeneratorMethod]
    public RelevantPoint GetRelevantObjects(RelevantHitObject relevantHitObject) {
        var ho = relevantHitObject.HitObject;
        if (ho.CurvePoints == null || ho.CurvePoints.Count == 0)
            return null;
        return ho.IsSlider ? new RelevantPoint(ho.CurvePoints.Last()) { CustomTime = ho.EndTime } : null;
    }
}