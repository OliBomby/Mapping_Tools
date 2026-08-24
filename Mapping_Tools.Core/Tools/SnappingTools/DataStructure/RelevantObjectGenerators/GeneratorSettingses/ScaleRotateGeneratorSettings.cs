using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;

namespace Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorSettingses;

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

    /// <inheritdoc />
    public override object Clone()
    {
        return new ScaleRotateGeneratorSettings
        {
            Generator = Generator, IsActive = IsActive, IsSequential = IsSequential, IsDeep = IsDeep,
            RelevancyRatio = RelevancyRatio, GeneratesInheritable = GeneratesInheritable,
            InputPredicate = (SelectionPredicateCollection)InputPredicate.Clone(), Angle = Angle, Scalar = Scalar,
            OriginInputPredicate = (SelectionPredicateCollection)OriginInputPredicate.Clone(),
            OtherInputPredicate = (SelectionPredicateCollection)OtherInputPredicate.Clone(),
        };
    }
}

