using System.Globalization;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.SystemTools;
using Mapping_Tools.Core.Tools.PatternGallery;
using Mapping_Tools.Desktop.Views.Dialogs;

namespace Mapping_Tools.Desktop.Interactions;

/// <summary>Carries submitted raw-code import values.</summary>
public sealed record PatternGalleryCodeInput(
    string Name,
    string HitObjects,
    string TimingPoints,
    double GlobalSv,
    GameMode GameMode);

