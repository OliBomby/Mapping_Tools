using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorSettingses;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Generators;

/// <summary>Generates a configurable circle centered on each point.</summary>
public sealed class SinglePointCircleGenerator : RelevantObjectsGenerator
{
    /// <summary>Creates the inactive generator with a 100-pixel radius.</summary>
    public SinglePointCircleGenerator() : base(new SinglePointCircleGeneratorSettings())
    {
        Settings.Generator = this;
        Settings.IsActive = false;
        Settings.IsDeep = false;
        Settings.InputPredicate.Predicates.Add(new SelectionPredicate { NeedSelected = true, MinRelevancy = 0.5 });
        MySettings.Radius = 100;
    }

    /// <inheritdoc />
    public override string Name => "Circle from Single Point";

    /// <inheritdoc />
    public override string Description => "Generates circles with a specified radius on every virtual point.";

    /// <inheritdoc />
    public override GeneratorType GeneratorType => GeneratorType.Intermediate;

    private SinglePointCircleGeneratorSettings MySettings => (SinglePointCircleGeneratorSettings)Settings;

    /// <summary>Generates a circle centered at the input point.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantCircle GetRelevantObjects(RelevantPoint point)
    {
        return new RelevantCircle(new Circle(point.Child, MySettings.Radius));
    }
}
