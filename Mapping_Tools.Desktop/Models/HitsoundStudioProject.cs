using ApplicationHitsoundStudioProject = Mapping_Tools.Application.Tools.HitsoundStudio.Models.HitsoundStudioProject;

namespace Mapping_Tools.Desktop.Models;

/// <summary>Stores Hitsound Studio presentation preferences alongside service inputs.</summary>
public sealed class HitsoundStudioProject : ApplicationHitsoundStudioProject
{
    /// <summary>Gets or sets whether the detailed export summary is shown after completion.</summary>
    public bool ShowResults { get; set; }

    /// <summary>Creates an independent copy of the project, including its Desktop-only display state.</summary>
    /// <returns>A project whose mutable application data is not shared with this instance.</returns>
    public new HitsoundStudioProject Clone()
    {
        ApplicationHitsoundStudioProject copy = base.Clone();
        return new HitsoundStudioProject
        {
            BaseBeatmap = copy.BaseBeatmap,
            DefaultSample = copy.DefaultSample,
            ExportFolder = copy.ExportFolder,
            HitsoundDiffName = copy.HitsoundDiffName,
            ExportMap = copy.ExportMap,
            ExportSamples = copy.ExportSamples,
            DeleteAllInExportFirst = copy.DeleteAllInExportFirst,
            UsePreviousSampleSchema = copy.UsePreviousSampleSchema,
            AllowGrowthPreviousSampleSchema = copy.AllowGrowthPreviousSampleSchema,
            AddCoincidingRegularHitsounds = copy.AddCoincidingRegularHitsounds,
            AddGreenLineVolumeToMidi = copy.AddGreenLineVolumeToMidi,
            PreviousSampleSchema = copy.PreviousSampleSchema,
            HitsoundExportModeSetting = copy.HitsoundExportModeSetting,
            HitsoundExportGameMode = copy.HitsoundExportGameMode,
            ZipLayersLeniency = copy.ZipLayersLeniency,
            FirstCustomIndex = copy.FirstCustomIndex,
            SingleSampleExportFormat = copy.SingleSampleExportFormat,
            MixedSampleExportFormat = copy.MixedSampleExportFormat,
            HitsoundLayers = copy.HitsoundLayers,
            ShowResults = ShowResults,
        };
    }
}
