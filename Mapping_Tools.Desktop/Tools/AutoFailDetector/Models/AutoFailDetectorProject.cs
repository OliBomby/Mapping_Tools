namespace Mapping_Tools.Desktop.Tools.AutoFailDetector.Models;

/// <summary>Stores the Auto-fail Detector options independently of analysis results.</summary>
public sealed class AutoFailDetectorProject
{
    /// <summary>Gets or sets whether confirmed unloading objects appear on the timeline.</summary>
    public bool ShowUnloadingObjects { get; set; } = true;

    /// <summary>Gets or sets whether possible unloading objects appear on the timeline.</summary>
    public bool ShowPotentialUnloadingObjects { get; set; }

    /// <summary>Gets or sets whether disrupting objects appear on the timeline.</summary>
    public bool ShowPotentialDisruptors { get; set; }

    /// <summary>Gets or sets the simulated approach rate, or -1 to use the map value.</summary>
    public double ApproachRateOverride { get; set; } = -1;

    /// <summary>Gets or sets the simulated overall difficulty, or -1 to use the map value.</summary>
    public double OverallDifficultyOverride { get; set; } = -1;

    /// <summary>Gets or sets the tolerated physics-update delay in milliseconds.</summary>
    public int PhysicsUpdateLeniency { get; set; } = 9;

    /// <summary>Gets or sets whether analysis offers repair guidance.</summary>
    public bool GetAutoFailFix { get; set; }

    /// <summary>Gets or sets whether an accepted repair may insert spinners automatically.</summary>
    public bool AutoPlaceFix { get; set; }
}
