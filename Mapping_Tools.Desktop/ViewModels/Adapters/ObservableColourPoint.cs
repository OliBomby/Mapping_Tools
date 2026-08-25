using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Mapping_Tools.Core.Tools.ComboColourStudio.Models;

namespace Mapping_Tools.Desktop.ViewModels.Adapters;

/// <summary>Adapts a plain combo-colour point for Desktop grid editing.</summary>
public sealed partial class ObservableColourPoint : ObservableObject
{
    /// <summary>Creates an adapter around the supplied point.</summary>
    /// <param name="model">The persisted point edited by this adapter.</param>
    public ObservableColourPoint(ColourPoint model)
    {
        this.Model = model ?? throw new ArgumentNullException(nameof(model));
        Time = model.Time;
        Mode = model.Mode;
        ColourSequence = new ObservableCollection<ObservableSpecialColour>(
            model.ColourSequence.Select(colour => new ObservableSpecialColour(colour)));
    }

    /// <summary>Gets the plain point represented by this adapter.</summary>
    public ColourPoint Model { get; }

    /// <summary>Gets or sets the point offset in milliseconds.</summary>
    [ObservableProperty]
    public partial double Time { get; set; }

    /// <summary>Gets or sets whether this point is normal or burst mode.</summary>
    [ObservableProperty]
    public partial ColourPointMode Mode { get; set; }

    /// <summary>Gets the available point application modes.</summary>
    public IReadOnlyList<ColourPointMode> ColourPointModes { get; } =
        Enum.GetValues<ColourPointMode>();

    /// <summary>Gets the editable ordered combo-colour sequence.</summary>
    public ObservableCollection<ObservableSpecialColour> ColourSequence { get; }

    /// <summary>Creates a plain snapshot including sequence edits.</summary>
    /// <returns>An independently mutable point for Application services.</returns>
    public ColourPoint Snapshot()
    {
        return new ColourPoint(
            Time,
            ColourSequence.Select(colour => colour.Snapshot()),
            Mode);
    }

    partial void OnTimeChanged(double value)
    {
        Model.Time = value;
    }

    partial void OnModeChanged(ColourPointMode value)
    {
        Model.Mode = value;
    }
}
