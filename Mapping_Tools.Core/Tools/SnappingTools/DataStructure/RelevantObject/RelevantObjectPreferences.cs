using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.Layers;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;
using Newtonsoft.Json;

namespace Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObject;

/// <summary>Stores the rendering preferences associated with one geometry kind.</summary>
public sealed class RelevantObjectPreferences : ICloneable
{
    /// <summary>Gets or sets the display name of this preference group.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the neutral ARGB colour consumed by a frontend renderer.</summary>
    public RgbaColour Color { get; set; }

    /// <summary>Gets or sets the base opacity multiplier.</summary>
    public double Opacity { get; set; }

    /// <summary>Gets or sets the line thickness in frontend-independent pixels.</summary>
    public double Thickness { get; set; }

    /// <summary>Gets or sets the dash pattern selected by the user.</summary>
    public DashStylesEnum Dashstyle { get; set; }

    /// <summary>Gets or sets the point radius/size when this kind supports one.</summary>
    public double Size { get; set; }

    /// <summary>Gets or sets whether the size setting applies to this geometry kind.</summary>
    public bool HasSizeOption { get; set; }

    /// <inheritdoc />
    public object Clone()
    {
        return MemberwiseClone();
    }
}

