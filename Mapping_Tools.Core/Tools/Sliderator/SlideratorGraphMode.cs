using Mapping_Tools.Core.Graph;
using Mapping_Tools.Core.MathUtil;
using Newtonsoft.Json;

namespace Mapping_Tools.Core.Tools.Sliderator;

/// <summary>Chooses whether graph values represent position or velocity.</summary>
public enum SlideratorGraphMode
{
    /// <summary>Interprets graph values as normalized slider position.</summary>
    Position,

    /// <summary>Interprets graph values as slider velocity in SV units.</summary>
    Velocity,
}

