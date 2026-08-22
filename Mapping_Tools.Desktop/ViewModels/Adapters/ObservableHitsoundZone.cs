using CommunityToolkit.Mvvm.ComponentModel;
using Mapping_Tools.Core.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Core.Classes.HitsoundStuff;

namespace Mapping_Tools.Desktop.ViewModels.Adapters;

/// <summary>
/// Adds Desktop change notification and transient list selection to a plain
/// <see cref="HitsoundZone"/> persistence model.
/// </summary>
public sealed class ObservableHitsoundZone : ObservableObject
{
    private readonly HitsoundZone model;
    private bool isSelected;

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
    }

    /// <summary>Gets the plain model currently edited by the adapter.</summary>
    public HitsoundZone Model => model;

    /// <summary>Gets or sets transient list selection state.</summary>
    public bool IsSelected
    {
        get => isSelected;
        set => SetProperty(ref isSelected, value);
    }

    /// <summary>Gets or sets the user-facing zone name.</summary>
    public string Name
    {
        get => model.Name;
        set
        {
            if (model.Name == value)
            {
                return;
            }

            model.Name = value ?? string.Empty;
            OnPropertyChanged();
        }
    }

    /// <summary>Gets or sets the optional explicit sample filename.</summary>
    public string Filename
    {
        get => model.Filename;
        set
        {
            if (model.Filename == value)
            {
                return;
            }

            model.Filename = value ?? string.Empty;
            OnPropertyChanged();
        }
    }

    /// <summary>Gets or sets the target playfield X coordinate, or -1 for wildcard.</summary>
    public double XPos
    {
        get => model.XPos;
        set
        {
            if (model.XPos == value)
            {
                return;
            }

            model.XPos = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Gets or sets the target playfield Y coordinate, or -1 for wildcard.</summary>
    public double YPos
    {
        get => model.YPos;
        set
        {
            if (model.YPos == value)
            {
                return;
            }

            model.YPos = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Gets or sets the hitsound layer matched by this zone.</summary>
    public Hitsound Hitsound
    {
        get => model.Hitsound;
        set
        {
            if (model.Hitsound == value)
            {
                return;
            }

            model.Hitsound = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Gets or sets the normal-layer sample family.</summary>
    public SampleSet SampleSet
    {
        get => model.SampleSet;
        set
        {
            if (model.SampleSet == value)
            {
                return;
            }

            model.SampleSet = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Gets or sets the addition-layer sample family.</summary>
    public SampleSet AdditionsSet
    {
        get => model.AdditionsSet;
        set
        {
            if (model.AdditionsSet == value)
            {
                return;
            }

            model.AdditionsSet = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Gets or sets the custom sample index assigned by the zone.</summary>
    public int CustomIndex
    {
        get => model.CustomIndex;
        set
        {
            if (model.CustomIndex == value)
            {
                return;
            }

            model.CustomIndex = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Creates a plain snapshot without transient selection state.</summary>
    /// <returns>An independently mutable zone suitable for an Application service.</returns>
    public HitsoundZone Snapshot() => model.Copy();
}
