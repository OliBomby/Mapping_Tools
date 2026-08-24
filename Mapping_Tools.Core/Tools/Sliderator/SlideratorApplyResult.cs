using System.Globalization;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.BeatmapHelper.SliderPathStuff;
using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Core.Tools.Sliderator;

/// <summary>Describes the generated Sliderator output before it is persisted.</summary>
/// <param name="NewLength">The output object's serialized pixel length.</param>
/// <param name="NewVelocity">The output object's effective travel rate.</param>
/// <param name="Simplified">Whether the source slider shape was reused.</param>
/// <param name="ObjectCount">The number of objects emitted by the operation.</param>
public sealed record SlideratorApplyResult(
    double NewLength,
    double NewVelocity,
    bool Simplified,
    int ObjectCount);

