using CommunityToolkit.Mvvm.ComponentModel;
using Mapping_Tools.Core.BeatmapHelper;

namespace Mapping_Tools.Desktop.ViewModels.Adapters;

/// <summary>Adapts a plain special colour for Desktop palette display and editing.</summary>
public sealed partial class ObservableSpecialColour : ObservableObject
{
    /// <summary>Creates an adapter around the supplied palette colour.</summary>
    /// <param name="model">The plain colour edited by this adapter.</param>
    public ObservableSpecialColour(SpecialColour model)
    {
        this.Model = model ?? throw new ArgumentNullException(nameof(model));
        Name = model.Name;
        Color = model.Color;
    }

    /// <summary>Gets the plain colour represented by this adapter.</summary>
    public SpecialColour Model { get; }

    /// <summary>Gets or sets the named colour key.</summary>
    [ObservableProperty]
    public partial string? Name { get; set; }

    /// <summary>Gets or sets the ARGB colour value.</summary>
    [ObservableProperty]
    public partial RgbaColour Color { get; set; }

    /// <summary>Gets the plain colour for persistence or an Application service.</summary>
    /// <returns>The underlying mutable colour model.</returns>
    public SpecialColour Snapshot()
    {
        return (SpecialColour)Model.Clone();
    }

    partial void OnNameChanged(string? value)
    {
        Model.Name = value;
    }

    partial void OnColorChanged(RgbaColour value)
    {
        Model.Color = value;
    }
}
