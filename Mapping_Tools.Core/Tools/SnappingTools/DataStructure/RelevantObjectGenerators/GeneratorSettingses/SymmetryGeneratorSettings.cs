using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;

namespace Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorSettingses;

/// <summary>Settings for reflection across a selected axis.</summary>
public sealed class SymmetryGeneratorSettings : GeneratorSettings
{
    /// <summary>Gets or sets the predicate for axis lines.</summary>
    public SelectionPredicateCollection AxisInputPredicate { get; set; } = new();

    /// <summary>Gets or sets the predicate for objects to reflect.</summary>
    public SelectionPredicateCollection OtherInputPredicate { get; set; } = new();

    /// <inheritdoc />
    public override object Clone()
    {
        return new SymmetryGeneratorSettings
        {
            Generator = Generator, IsActive = IsActive, IsSequential = IsSequential, IsDeep = IsDeep,
            RelevancyRatio = RelevancyRatio, GeneratesInheritable = GeneratesInheritable,
            InputPredicate = (SelectionPredicateCollection)InputPredicate.Clone(),
            AxisInputPredicate = (SelectionPredicateCollection)AxisInputPredicate.Clone(),
            OtherInputPredicate = (SelectionPredicateCollection)OtherInputPredicate.Clone(),
        };
    }
}
