using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;

namespace Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorSettingses;

/// <summary>Settings for a circle generated from one point.</summary>
public sealed class SinglePointCircleGeneratorSettings : GeneratorSettings
{
    /// <summary>Gets or sets the circle radius in editor pixels.</summary>
    public double Radius { get; set; }

    /// <inheritdoc />
    public override object Clone()
    {
        return new SinglePointCircleGeneratorSettings
        {
            Generator = Generator, IsActive = IsActive, IsSequential = IsSequential, IsDeep = IsDeep,
            RelevancyRatio = RelevancyRatio, GeneratesInheritable = GeneratesInheritable,
            InputPredicate = (SelectionPredicateCollection)InputPredicate.Clone(), Radius = Radius,
        };
    }
}

