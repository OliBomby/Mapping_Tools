using Mapping_Tools.Core.BeatmapHelper.Enums;

namespace Mapping_Tools.Desktop.Tools.PatternGallery.Models;

/// <summary>Carries submitted raw-code import values.</summary>
public sealed record PatternGalleryCodeInput(
    string Name,
    string HitObjects,
    string TimingPoints,
    double GlobalSv,
    GameMode GameMode);
