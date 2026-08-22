using CommunityToolkit.Mvvm.ComponentModel;
using Mapping_Tools.Core.Classes.BeatmapHelper;

namespace Mapping_Tools.Desktop.ViewModels.Adapters;

/// <summary>Adapts a plain special colour for Desktop palette display and editing.</summary>
public sealed class ObservableSpecialColour : ObservableObject
{
    private readonly SpecialColour model;

    /// <summary>Creates an adapter around the supplied palette colour.</summary>
    /// <param name="model">The plain colour edited by this adapter.</param>
    public ObservableSpecialColour(SpecialColour model)
    {
        this.model = model ?? throw new ArgumentNullException(nameof(model));
    }

    /// <summary>Gets the plain colour represented by this adapter.</summary>
    public SpecialColour Model => model;

    /// <summary>Gets or sets the named colour key.</summary>
    public string? Name
    {
        get => model.Name;
        set
        {
            if (model.Name == value) return;
            model.Name = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Gets or sets the ARGB colour value.</summary>
    public RgbaColour Color
    {
        get => model.Color;
        set
        {
            if (model.Color == value) return;
            model.Color = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Gets the plain colour for persistence or an Application service.</summary>
    /// <returns>The underlying mutable colour model.</returns>
    public SpecialColour Snapshot() => (SpecialColour)model.Clone();
}
