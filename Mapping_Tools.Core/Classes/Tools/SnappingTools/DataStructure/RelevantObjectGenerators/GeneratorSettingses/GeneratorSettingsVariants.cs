using Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;

namespace Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorSettingses;

/// <summary>Settings for the scale-and-rotate generator.</summary>
public sealed class ScaleRotateGeneratorSettings : GeneratorSettings
{
    private double _angle;
    private double _scalar = 1;
    private SelectionPredicateCollection _originInputPredicate = new();
    private SelectionPredicateCollection _otherInputPredicate = new();

    /// <summary>Gets or sets the rotation angle in degrees.</summary>
    public double Angle { get => _angle; set => Set(ref _angle, value); }

    /// <summary>Gets or sets the uniform scale factor.</summary>
    public double Scalar { get => _scalar; set => Set(ref _scalar, value); }

    /// <summary>Gets or sets the predicate for origin lines/points.</summary>
    public SelectionPredicateCollection OriginInputPredicate { get => _originInputPredicate; set => Set(ref _originInputPredicate, value); }

    /// <summary>Gets or sets the predicate for transformed objects.</summary>
    public SelectionPredicateCollection OtherInputPredicate { get => _otherInputPredicate; set => Set(ref _otherInputPredicate, value); }

    /// <inheritdoc/>
    public override object Clone() => new ScaleRotateGeneratorSettings
    {
        Generator = Generator, IsActive = IsActive, IsSequential = IsSequential, IsDeep = IsDeep,
        RelevancyRatio = RelevancyRatio, GeneratesInheritable = GeneratesInheritable,
        InputPredicate = (SelectionPredicateCollection)InputPredicate.Clone(), Angle = Angle, Scalar = Scalar,
        OriginInputPredicate = (SelectionPredicateCollection)OriginInputPredicate.Clone(),
        OtherInputPredicate = (SelectionPredicateCollection)OtherInputPredicate.Clone()
    };
}

/// <summary>Settings for a circle generated from one point.</summary>
public sealed class SinglePointCircleGeneratorSettings : GeneratorSettings
{
    private double _radius;

    /// <summary>Gets or sets the circle radius in editor pixels.</summary>
    public double Radius { get => _radius; set => Set(ref _radius, value); }

    /// <inheritdoc/>
    public override object Clone() => new SinglePointCircleGeneratorSettings
    {
        Generator = Generator, IsActive = IsActive, IsSequential = IsSequential, IsDeep = IsDeep,
        RelevancyRatio = RelevancyRatio, GeneratesInheritable = GeneratesInheritable,
        InputPredicate = (SelectionPredicateCollection)InputPredicate.Clone(), Radius = Radius
    };
}

/// <summary>Settings for slider-path point sampling.</summary>
public sealed class SliderPathGeneratorSettings : GeneratorSettings
{
    private double _pointDensity;

    /// <summary>Gets or sets the number of generated points per path pixel.</summary>
    public double PointDensity { get => _pointDensity; set => Set(ref _pointDensity, value); }

    /// <inheritdoc/>
    public override object Clone() => new SliderPathGeneratorSettings
    {
        Generator = Generator, IsActive = IsActive, IsSequential = IsSequential, IsDeep = IsDeep,
        RelevancyRatio = RelevancyRatio, GeneratesInheritable = GeneratesInheritable,
        InputPredicate = (SelectionPredicateCollection)InputPredicate.Clone(), PointDensity = PointDensity
    };
}

/// <summary>Settings for reflection across a selected axis.</summary>
public sealed class SymmetryGeneratorSettings : GeneratorSettings
{
    private SelectionPredicateCollection _axisInputPredicate = new();
    private SelectionPredicateCollection _otherInputPredicate = new();

    /// <summary>Gets or sets the predicate for axis lines.</summary>
    public SelectionPredicateCollection AxisInputPredicate { get => _axisInputPredicate; set => Set(ref _axisInputPredicate, value); }

    /// <summary>Gets or sets the predicate for objects to reflect.</summary>
    public SelectionPredicateCollection OtherInputPredicate { get => _otherInputPredicate; set => Set(ref _otherInputPredicate, value); }

    /// <inheritdoc/>
    public override object Clone() => new SymmetryGeneratorSettings
    {
        Generator = Generator, IsActive = IsActive, IsSequential = IsSequential, IsDeep = IsDeep,
        RelevancyRatio = RelevancyRatio, GeneratesInheritable = GeneratesInheritable,
        InputPredicate = (SelectionPredicateCollection)InputPredicate.Clone(),
        AxisInputPredicate = (SelectionPredicateCollection)AxisInputPredicate.Clone(),
        OtherInputPredicate = (SelectionPredicateCollection)OtherInputPredicate.Clone()
    };
}
