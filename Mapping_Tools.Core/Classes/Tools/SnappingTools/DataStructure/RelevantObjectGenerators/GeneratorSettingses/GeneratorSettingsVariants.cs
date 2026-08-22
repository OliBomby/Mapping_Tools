using Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;

namespace Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorSettingses;

/// <summary>Settings for the scale-and-rotate generator.</summary>
public sealed class ScaleRotateGeneratorSettings : GeneratorSettings
{
    /// <summary>Gets or sets the rotation angle in degrees.</summary>
    public double Angle { get; set; }

    /// <summary>Gets or sets the uniform scale factor.</summary>
    public double Scalar { get; set; } = 1;

    /// <summary>Gets or sets the predicate for origin lines/points.</summary>
    public SelectionPredicateCollection OriginInputPredicate { get; set; } = new();

    /// <summary>Gets or sets the predicate for transformed objects.</summary>
    public SelectionPredicateCollection OtherInputPredicate { get; set; } = new();

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
    /// <summary>Gets or sets the circle radius in editor pixels.</summary>
    public double Radius { get; set; }

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
    /// <summary>Gets or sets the number of generated points per path pixel.</summary>
    public double PointDensity { get; set; }

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
    /// <summary>Gets or sets the predicate for axis lines.</summary>
    public SelectionPredicateCollection AxisInputPredicate { get; set; } = new();

    /// <summary>Gets or sets the predicate for objects to reflect.</summary>
    public SelectionPredicateCollection OtherInputPredicate { get; set; } = new();

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
