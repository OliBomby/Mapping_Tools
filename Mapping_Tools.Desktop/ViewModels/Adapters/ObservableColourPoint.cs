using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Tools.ComboColourStudio;

namespace Mapping_Tools.Desktop.ViewModels.Adapters;

/// <summary>Adapts a plain combo-colour point for Desktop grid editing.</summary>
public sealed class ObservableColourPoint : ObservableObject
{
    private readonly ColourPoint model;

    /// <summary>Creates an adapter around the supplied point.</summary>
    /// <param name="model">The persisted point edited by this adapter.</param>
    public ObservableColourPoint(ColourPoint model)
    {
        this.model = model ?? throw new ArgumentNullException(nameof(model));
        ColourSequence = new ObservableCollection<ObservableSpecialColour>(
            model.ColourSequence.Select(colour => new ObservableSpecialColour(colour)));
    }

    /// <summary>Gets the plain point represented by this adapter.</summary>
    public ColourPoint Model => model;

    /// <summary>Gets or sets the point offset in milliseconds.</summary>
    public double Time
    {
        get => model.Time;
        set
        {
            if (model.Time == value) return;
            model.Time = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Gets or sets whether this point is normal or burst mode.</summary>
    public ColourPointMode Mode
    {
        get => model.Mode;
        set
        {
            if (model.Mode == value) return;
            model.Mode = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Gets the available point application modes.</summary>
    public IReadOnlyList<ColourPointMode> ColourPointModes { get; } =
        Enum.GetValues<ColourPointMode>();

    /// <summary>Gets the editable ordered combo-colour sequence.</summary>
    public ObservableCollection<ObservableSpecialColour> ColourSequence { get; }

    /// <summary>Creates a plain snapshot including sequence edits.</summary>
    /// <returns>An independently mutable point for Application services.</returns>
    public ColourPoint Snapshot() => new(
        Time,
        ColourSequence.Select(colour => colour.Snapshot()),
        Mode);
}
