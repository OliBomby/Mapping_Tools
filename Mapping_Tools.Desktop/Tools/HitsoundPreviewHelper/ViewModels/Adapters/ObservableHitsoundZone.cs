using CommunityToolkit.Mvvm.ComponentModel;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.HitsoundStuff;

namespace Mapping_Tools.Desktop.Tools.HitsoundPreviewHelper.ViewModels.Adapters;

/// <summary>
///     Adds Desktop change notification and transient list selection to a plain
///     <see cref="HitsoundZone" /> persistence model.
/// </summary>
public sealed partial class ObservableHitsoundZone : ObservableObject
{
    /// <summary>Creates an adapter around a new wildcard zone.</summary>
    public ObservableHitsoundZone()
        : this(new HitsoundZone())
    {
    }

    /// <summary>Creates an adapter around an existing plain zone.</summary>
    /// <param name="model">The domain snapshot to edit through this adapter.</param>
    public ObservableHitsoundZone(HitsoundZone model)
    {
        this.Model = model ?? throw new ArgumentNullException(nameof(model));
        Name = model.Name;
        Filename = model.Filename;
        XPos = model.XPos;
        YPos = model.YPos;
        Hitsound = model.Hitsound;
        SampleSet = model.SampleSet;
        AdditionsSet = model.AdditionsSet;
        CustomIndex = model.CustomIndex;
    }

    /// <summary>Gets the plain model currently edited by the adapter.</summary>
    public HitsoundZone Model { get; }

    /// <summary>Gets or sets transient list selection state.</summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    /// <summary>Gets or sets the user-facing zone name.</summary>
    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional explicit sample filename.</summary>
    [ObservableProperty]
    public partial string Filename { get; set; } = string.Empty;

    /// <summary>Gets or sets the target playfield X coordinate, or -1 for wildcard.</summary>
    [ObservableProperty]
    public partial double XPos { get; set; }

    /// <summary>Gets or sets the target playfield Y coordinate, or -1 for wildcard.</summary>
    [ObservableProperty]
    public partial double YPos { get; set; }

    /// <summary>Gets or sets the hitsound layer matched by this zone.</summary>
    [ObservableProperty]
    public partial Hitsound Hitsound { get; set; }

    /// <summary>Gets or sets the normal-layer sample family.</summary>
    [ObservableProperty]
    public partial SampleSet SampleSet { get; set; }

    /// <summary>Gets or sets the addition-layer sample family.</summary>
    [ObservableProperty]
    public partial SampleSet AdditionsSet { get; set; }

    /// <summary>Gets or sets the custom sample index assigned by the zone.</summary>
    [ObservableProperty]
    public partial int CustomIndex { get; set; }

    /// <summary>Creates a plain snapshot without transient selection state.</summary>
    /// <returns>An independently mutable zone suitable for an Application service.</returns>
    public HitsoundZone Snapshot()
    {
        return Model.Copy();
    }

    partial void OnNameChanged(string value)
    {
        Model.Name = value;
    }

    partial void OnFilenameChanged(string value)
    {
        Model.Filename = value;
    }

    partial void OnXPosChanged(double value)
    {
        Model.XPos = value;
    }

    partial void OnYPosChanged(double value)
    {
        Model.YPos = value;
    }

    partial void OnHitsoundChanged(Hitsound value)
    {
        Model.Hitsound = value;
    }

    partial void OnSampleSetChanged(SampleSet value)
    {
        Model.SampleSet = value;
    }

    partial void OnAdditionsSetChanged(SampleSet value)
    {
        Model.AdditionsSet = value;
    }

    partial void OnCustomIndexChanged(int value)
    {
        Model.CustomIndex = value;
    }
}
