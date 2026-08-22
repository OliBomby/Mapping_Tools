using CommunityToolkit.Mvvm.ComponentModel;
using Mapping_Tools.Core.Classes.BeatmapHelper;

namespace Mapping_Tools.Desktop.ViewModels.Adapters;

/// <summary>Adapts a plain combo colour for Metadata Manager editing.</summary>
public sealed class ObservableComboColour : ObservableObject
{
    private readonly ComboColour model;

    /// <summary>Creates an adapter around the supplied combo colour.</summary>
    /// <param name="model">The plain colour edited by this adapter.</param>
    public ObservableComboColour(ComboColour model)
    {
        this.model = model ?? throw new ArgumentNullException(nameof(model));
    }

    /// <summary>Gets the plain colour represented by this adapter.</summary>
    public ComboColour Model => model;

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

    /// <summary>Creates a plain colour snapshot.</summary>
    /// <returns>An independent combo colour.</returns>
    public ComboColour Snapshot() => new(Color);
}
