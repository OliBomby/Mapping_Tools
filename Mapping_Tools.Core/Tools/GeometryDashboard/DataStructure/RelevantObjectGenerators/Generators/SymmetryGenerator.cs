using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorSettingses;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Generators;

/// <summary>Generates reflected points, lines, and circles across a selected axis.</summary>
public sealed class SymmetryGenerator : RelevantObjectsGenerator
{
    /// <summary>Creates the active deep reflection generator with legacy defaults.</summary>
    public SymmetryGenerator() : base(new SymmetryGeneratorSettings())
    {
        Settings.Generator = this;
        Settings.RelevancyRatio = 0.8;
        Settings.IsActive = true;
        Settings.IsDeep = true;
        MySettings.AxisInputPredicate.Predicates.Add(new SelectionPredicate { NeedSelected = true, NeedLocked = true, NeedGeneratedNotByThis = true });
    }

    /// <inheritdoc />
    public override string Name => "Reflection across a Line";

    /// <inheritdoc />
    public override string Tooltip =>
        "Mirrors any virtual object over a virtual line where the virtual line is the symmetry axis. In the settings you can set extra rules for selecting the symmetry axis.";

    /// <inheritdoc />
    public override GeneratorType GeneratorType => GeneratorType.Advanced;

    private SymmetryGeneratorSettings MySettings => (SymmetryGeneratorSettings)Settings;

    /// <summary>Reflects a point across an axis.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantPoint? GetRelevantObjects(RelevantLine axis, RelevantPoint point)
    {
        return !MySettings.AxisInputPredicate.Check(axis, this) || !MySettings.OtherInputPredicate.Check(point, this)
            ? null
            : new RelevantPoint(Vector2.Mirror(point.Child, axis.Child));
    }

    /// <summary>Reflects one line across another line.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantLine? GetRelevantObjects(RelevantLine line1, RelevantLine line2)
    {
        if (MySettings.AxisInputPredicate.Check(line1, this) && MySettings.OtherInputPredicate.Check(line2, this)) return ReflectedLine(line1, line2);
        if (MySettings.AxisInputPredicate.Check(line2, this) && MySettings.OtherInputPredicate.Check(line1, this)) return ReflectedLine(line2, line1);
        return null;
    }

    /// <summary>Reflects a circle across an axis.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantCircle? GetRelevantObjects(RelevantLine axis, RelevantCircle circle)
    {
        return !MySettings.AxisInputPredicate.Check(axis, this) || !MySettings.OtherInputPredicate.Check(circle, this)
            ? null
            : new RelevantCircle(new Circle(Vector2.Mirror(circle.Child.Centre, axis.Child), circle.Child.Radius));
    }

    private static RelevantLine ReflectedLine(RelevantLine axis, RelevantLine line)
    {
        return new RelevantLine(Line2.FromPoints(Vector2.Mirror(line.Child.PositionVector, axis.Child),
            Vector2.Mirror(line.Child.PositionVector + line.Child.DirectionVector, axis.Child)));
    }
}

