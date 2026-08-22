using CommunityToolkit.Mvvm.ComponentModel;
using Mapping_Tools.Core.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Core.Classes.HitsoundStuff;

namespace Mapping_Tools.Desktop.ViewModels.Adapters;

/// <summary>
/// Adds Desktop change notification and transient list selection to a plain
/// <see cref="HitsoundZone"/> persistence model.
/// </summary>
public sealed partial class ObservableHitsoundZone : ObservableObject
{
    private readonly HitsoundZone model;

    /// <summary>Creates an adapter around a new wildcard zone.</summary>
    public ObservableHitsoundZone()
        : this(new HitsoundZone())
    {
    }

    /// <summary>Creates an adapter around an existing plain zone.</summary>
    /// <param name="model">The domain snapshot to edit through this adapter.</param>
    public ObservableHitsoundZone(HitsoundZone model)
    {
        this.model = model ?? throw new ArgumentNullException(nameof(model));
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
    public HitsoundZone Model => model;

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
    public HitsoundZone Snapshot() => model.Copy();

    partial void OnNameChanged(string value) => model.Name = value;
    partial void OnFilenameChanged(string value) => model.Filename = value;
    partial void OnXPosChanged(double value) => model.XPos = value;
    partial void OnYPosChanged(double value) => model.YPos = value;
    partial void OnHitsoundChanged(Hitsound value) => model.Hitsound = value;
    partial void OnSampleSetChanged(SampleSet value) => model.SampleSet = value;
    partial void OnAdditionsSetChanged(SampleSet value) => model.AdditionsSet = value;
    partial void OnCustomIndexChanged(int value) => model.CustomIndex = value;
}
