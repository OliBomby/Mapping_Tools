using CommunityToolkit.Mvvm.ComponentModel;
using Mapping_Tools.Core.Classes.BeatmapHelper;

namespace Mapping_Tools.Desktop.ViewModels.Adapters;

/// <summary>Adapts a plain combo colour for Metadata Manager editing.</summary>
public sealed partial class ObservableComboColour : ObservableObject
{
    private readonly ComboColour model;

    /// <summary>Creates an adapter around the supplied combo colour.</summary>
    /// <param name="model">The plain colour edited by this adapter.</param>
    public ObservableComboColour(ComboColour model)
    {
        this.model = model ?? throw new ArgumentNullException(nameof(model));
        Color = model.Color;
    }

    /// <summary>Gets the plain colour represented by this adapter.</summary>
    public ComboColour Model => model;

    /// <summary>Gets or sets the ARGB colour value.</summary>
    [ObservableProperty]
    public partial RgbaColour Color { get; set; }

    /// <summary>Creates a plain colour snapshot.</summary>
    /// <returns>An independent combo colour.</returns>
    public ComboColour Snapshot() => new(Color);

    partial void OnColorChanged(RgbaColour value) => model.Color = value;
}
