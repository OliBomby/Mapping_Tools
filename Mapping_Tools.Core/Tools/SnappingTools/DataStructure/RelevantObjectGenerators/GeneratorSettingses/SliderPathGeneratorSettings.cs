using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;

namespace Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorSettingses;

/// <summary>Settings for slider-path point sampling.</summary>
public sealed class SliderPathGeneratorSettings : GeneratorSettings
{
    /// <summary>Gets or sets the number of generated points per path pixel.</summary>
    public double PointDensity { get; set; }

    /// <inheritdoc />
    public override object Clone()
    {
        return new SliderPathGeneratorSettings
        {
            Generator = Generator, IsActive = IsActive, IsSequential = IsSequential, IsDeep = IsDeep,
            RelevancyRatio = RelevancyRatio, GeneratesInheritable = GeneratesInheritable,
            InputPredicate = (SelectionPredicateCollection)InputPredicate.Clone(), PointDensity = PointDensity,
        };
    }
}

