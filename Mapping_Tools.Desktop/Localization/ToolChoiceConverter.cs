using System.Globalization;
using Avalonia.Data.Converters;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tools.HitsoundStudio.Models;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.HitsoundStuff;
using Mapping_Tools.Core.Tools.ComboColourStudio.Models;
using Mapping_Tools.Core.Tools.GeometryDashboard;
using Mapping_Tools.Core.Tools.HitsoundCopier.Models;
using Mapping_Tools.Core.Tools.PatternGallery.Models;
using Mapping_Tools.Core.Tools.RhythmGuide.Models;
using Mapping_Tools.Core.Tools.SliderCompletionator.Models;
using Mapping_Tools.Core.Tools.SliderMerger.Models;
using Mapping_Tools.Core.Tools.TimingCopier.Models;
using Mapping_Tools.Core.Tools.TumourGenerator.Models;
using Mapping_Tools.Core.Tools.TumourGenerator.Templates;

namespace Mapping_Tools.Desktop.Localization;

/// <summary>Displays translated labels for enum and string choices.</summary>
public sealed class ToolChoiceConverter : IValueConverter
{
    /// <summary>Gets the singleton converter used by tool views.</summary>
    public static ToolChoiceConverter Instance { get; } = new();

    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string option)
        {
            return option switch
            {
                "<Current Tool>" => DesktopStrings.Shell_CurrentTool,
                "Name" => DesktopStrings.Common_Name,
                "Creation time" => DesktopStrings.Common_CreationTime,
                "Last used time" => DesktopStrings.Common_LastUsedTime,
                "Usage count" => DesktopStrings.Common_UsageCount,
                "Object count" => DesktopStrings.Common_ObjectCount,
                "Duration" => DesktopStrings.Common_Duration,
                "Beat length" => DesktopStrings.Common_BeatLength,
                _ => option,
            };
        }

        return value switch
        {
            CurrentBeatmapFetchingMode.Disabled => DesktopStrings.Shell_Disabled,
            CurrentBeatmapFetchingMode.MemoryRead => DesktopStrings.Shell_MemoryRead,
            CurrentBeatmapFetchingMode.Mtipc => "MTIPC",
            CurrentBeatmapFetchingMode.Gosumemory => "Gosumemory/Tosu",
            BeatmapLiveStateReadingMode.Disabled => DesktopStrings.Shell_Disabled,
            BeatmapLiveStateReadingMode.EditorReader => DesktopStrings.Shell_MemoryRead,
            BeatmapLiveStateReadingMode.Mtipc => "MTIPC",
            EditorReloadMode.Disabled => DesktopStrings.Shell_Disabled,
            EditorReloadMode.SimulatedKeypress => DesktopStrings.Shell_SimulatedKeypress,
            EditorReloadMode.Mtipc => "MTIPC",
            HitsoundCopierCopyMode.OverwriteEverything => DesktopStrings.HitsoundCopier_OverwriteEverything,
            HitsoundCopierCopyMode.OverwriteOnlyDefined => DesktopStrings.HitsoundCopier_OverwriteDefined,
            HitsoundStudioSampleExportFormat.Default => DesktopStrings.HitsoundStudio_FormatDefault,
            HitsoundStudioSampleExportFormat.WaveIeeeFloat => DesktopStrings.HitsoundStudio_FormatFloat,
            HitsoundStudioSampleExportFormat.WavePcm => DesktopStrings.HitsoundStudio_FormatPcm,
            HitsoundStudioSampleExportFormat.OggVorbis => DesktopStrings.HitsoundStudio_FormatVorbis,
            HitsoundStudioSampleExportFormat.MidiChords => DesktopStrings.HitsoundStudio_FormatMidi,
            HitsoundStudioExportMode.Standard => DesktopStrings.HitsoundStudio_ExportStandard,
            HitsoundStudioExportMode.Coinciding => DesktopStrings.HitsoundStudio_ExportCoinciding,
            HitsoundStudioExportMode.Storyboard => DesktopStrings.HitsoundStudio_ExportStoryboard,
            HitsoundStudioExportMode.Midi => DesktopStrings.HitsoundStudio_ExportMidi,
            ImportType.None => DesktopStrings.HitsoundStudio_ImportNone,
            ImportType.Stack => DesktopStrings.HitsoundStudio_ImportStack,
            ImportType.Hitsounds => DesktopStrings.HitsoundStudio_ImportType_Hitsounds,
            ImportType.MIDI => DesktopStrings.HitsoundStudio_ImportType_Midi,
            ImportType.Storyboard => DesktopStrings.HitsoundStudio_ImportType_Storyboard,
            SampleSet.None => DesktopStrings.HitsoundStudio_SampleNone,
            ColourPointMode.Normal => DesktopStrings.ComboColourStudio_ModeNormal,
            ColourPointMode.Burst => DesktopStrings.ComboColourStudio_ModeBurst,
            DashStylesEnum.Dash => DesktopStrings.GeometryDashboard_DashStyle_Dash,
            DashStylesEnum.Dot => DesktopStrings.GeometryDashboard_DashStyle_Dot,
            DashStylesEnum.DashSingleDot => DesktopStrings.GeometryDashboard_DashStyle_DashSingleDot,
            DashStylesEnum.DashDoubleDot => DesktopStrings.GeometryDashboard_DashStyle_DashDoubleDot,
            DashStylesEnum.Solid => DesktopStrings.GeometryDashboard_DashStyle_Solid,
            TimingCopierResnapMode.PreserveBeatSpacing => DesktopStrings.TimingCopier_ModeBeats,
            TimingCopierResnapMode.Resnap => DesktopStrings.TimingCopier_ModeResnap,
            TimingCopierResnapMode.KeepObjectsFixed => DesktopStrings.TimingCopier_ModeFixed,
            ExportTimeMode.Current => DesktopStrings.PatternGallery_ExportTimeMode_Current,
            ExportTimeMode.Custom => DesktopStrings.PatternGallery_ExportTimeMode_Custom,
            ExportTimeMode.Pattern => DesktopStrings.PatternGallery_ExportTimeMode_Pattern,
            HitObjectSelectionMode.Bookmarked => DesktopStrings.Common_HitObjectSelectionMode_Bookmarked,
            HitObjectSelectionMode.Everything => DesktopStrings.Common_HitObjectSelectionMode_Everything,
            HitObjectSelectionMode.Selected => DesktopStrings.Common_HitObjectSelectionMode_Selected,
            HitObjectSelectionMode.Time => DesktopStrings.Common_HitObjectSelectionMode_Time,
            PatternOverwriteMode.CompleteOverwrite => DesktopStrings.PatternGallery_PatternOverwriteMode_CompleteOverwrite,
            PatternOverwriteMode.NoOverwrite => DesktopStrings.PatternGallery_PatternOverwriteMode_NoOverwrite,
            PatternOverwriteMode.PartitionedOverwrite => DesktopStrings.PatternGallery_PatternOverwriteMode_PartitionedOverwrite,
            RhythmGuideExportMode.AddToMap => DesktopStrings.RhythmGuide_ExportMode_AddToMap,
            RhythmGuideExportMode.NewMap => DesktopStrings.RhythmGuide_ExportMode_NewMap,
            RhythmGuideSelectionMode.AllEvents => DesktopStrings.RhythmGuide_SelectionMode_AllEvents,
            RhythmGuideSelectionMode.AllEventSeparated => DesktopStrings.RhythmGuide_SelectionMode_AllEventSeparated,
            RhythmGuideSelectionMode.HitsoundEvents => DesktopStrings.RhythmGuide_SelectionMode_HitsoundEvents,
            RhythmGuideSelectionMode.LongNotes => DesktopStrings.RhythmGuide_SelectionMode_LongNotes,
            SliderCompletionatorFreeVariable.Duration => DesktopStrings.SliderCompletionator_FreeVariable_Duration,
            SliderCompletionatorFreeVariable.Length => DesktopStrings.SliderCompletionator_FreeVariable_Length,
            SliderCompletionatorFreeVariable.Velocity => DesktopStrings.SliderCompletionator_FreeVariable_Velocity,
            SliderMergerConnectionMode.Linear => DesktopStrings.SliderMerger_ConnectionMode_Linear,
            SliderMergerConnectionMode.Move => DesktopStrings.SliderMerger_ConnectionMode_Move,
            TimingOverwriteMode.InPatternAbsoluteTiming => DesktopStrings.PatternGallery_TimingOverwriteMode_InPatternAbsoluteTiming,
            TimingOverwriteMode.InPatternRelativeTiming => DesktopStrings.PatternGallery_TimingOverwriteMode_InPatternRelativeTiming,
            TimingOverwriteMode.OriginalTimingOnly => DesktopStrings.PatternGallery_TimingOverwriteMode_OriginalTimingOnly,
            TimingOverwriteMode.PatternTimingOnly => DesktopStrings.PatternGallery_TimingOverwriteMode_PatternTimingOnly,
            TumourSidedness.AlternatingLeft => DesktopStrings.TumourGenerator_Sidedness_AlternatingLeft,
            TumourSidedness.AlternatingRight => DesktopStrings.TumourGenerator_Sidedness_AlternatingRight,
            TumourSidedness.Left => DesktopStrings.TumourGenerator_Sidedness_Left,
            TumourSidedness.Random => DesktopStrings.TumourGenerator_Sidedness_Random,
            TumourSidedness.Right => DesktopStrings.TumourGenerator_Sidedness_Right,
            TumourTemplate.Circle => DesktopStrings.TumourGenerator_Template_Circle,
            TumourTemplate.Parabola => DesktopStrings.TumourGenerator_Template_Parabola,
            TumourTemplate.Square => DesktopStrings.TumourGenerator_Template_Square,
            TumourTemplate.Triangle => DesktopStrings.TumourGenerator_Template_Triangle,
            WrappingMode.Absolute => DesktopStrings.TumourGenerator_WrappingMode_Absolute,
            WrappingMode.Simple => DesktopStrings.TumourGenerator_WrappingMode_Simple,
            WrappingMode.Wrap => DesktopStrings.TumourGenerator_WrappingMode_Wrap,
            // Keep osu! ruleset and sample-bank identifiers in their canonical form.
            SampleSet or Hitsound or GameMode => value.ToString() ?? string.Empty,
            Enum enumValue => enumValue.ToString(),
            _ => value?.ToString() ?? string.Empty,
        };
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return Avalonia.Data.BindingOperations.DoNothing;
    }
}
