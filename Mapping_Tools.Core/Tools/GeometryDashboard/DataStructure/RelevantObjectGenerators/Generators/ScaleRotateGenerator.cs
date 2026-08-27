using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorSettingses;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Generators;

/// <summary>Generates a transformed point, line, or circle around a selected origin.</summary>
public sealed class ScaleRotateGenerator : RelevantObjectsGenerator
{
    /// <summary>Creates the active deep transform generator with legacy defaults.</summary>
    public ScaleRotateGenerator() : base(new ScaleRotateGeneratorSettings())
    {
        Settings.Generator = this;
        Settings.RelevancyRatio = 0.8;
        Settings.IsActive = true;
        Settings.IsDeep = true;
        MySettings.Angle = 180;
        MySettings.Scalar = 1;
        MySettings.OriginInputPredicate.Predicates.Add(new SelectionPredicate { NeedSelected = true, NeedLocked = true, NeedGeneratedNotByThis = true });
    }

    /// <inheritdoc />
    public override string Name => "Scale & Rotate around a Fixed Point";

    /// <inheritdoc />
    public override string Tooltip =>
        "Spins and scales any virtual object around a fixed point by a specified angle and scalar. In the settings you can set the angle, scalar and extra rules for selecting the fixed point.";

    /// <inheritdoc />
    public override GeneratorType GeneratorType => GeneratorType.Advanced;

    private ScaleRotateGeneratorSettings MySettings => (ScaleRotateGeneratorSettings)Settings;

    /// <summary>Transforms a point around a selected point origin.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantPoint? GetRelevantObjects(RelevantPoint point1, RelevantPoint point2)
    {
        if (MySettings.OriginInputPredicate.Check(point1, this) && MySettings.OtherInputPredicate.Check(point2, this))
            return new RelevantPoint(Transform(point2.Child, point1.Child));
        if (MySettings.OriginInputPredicate.Check(point2, this) && MySettings.OtherInputPredicate.Check(point1, this))
            return new RelevantPoint(Transform(point1.Child, point2.Child));
        return null;
    }

    /// <summary>Transforms a line around a selected point origin.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantLine? GetRelevantObjects(RelevantPoint origin, RelevantLine line)
    {
        return !MySettings.OriginInputPredicate.Check(origin, this) || !MySettings.OtherInputPredicate.Check(line, this)
            ? null
            : new RelevantLine(
                Line2.FromPoints(Transform(line.Child.PositionVector, origin.Child), Transform(line.Child.PositionVector + line.Child.DirectionVector, origin.Child)));
    }

    /// <summary>Transforms a circle around a selected point origin.</summary>
    [RelevantObjectsGeneratorMethod]
    public RelevantCircle? GetRelevantObjects(RelevantPoint origin, RelevantCircle circle)
    {
        return !MySettings.OriginInputPredicate.Check(origin, this) || !MySettings.OtherInputPredicate.Check(circle, this)
            ? null
            : new RelevantCircle(new Circle(Transform(circle.Child.Centre, origin.Child), circle.Child.Radius * MySettings.Scalar));
    }

    private Vector2 Transform(Vector2 point, Vector2 origin)
    {
        return Matrix2.Mult(Matrix2.CreateRotation(MathHelper.DegreesToRadians(MySettings.Angle)), point - origin) * MySettings.Scalar + origin;
    }
}

