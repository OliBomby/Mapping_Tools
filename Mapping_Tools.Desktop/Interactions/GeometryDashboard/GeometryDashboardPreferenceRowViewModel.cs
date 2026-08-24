using System.Globalization;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.Tools.SnappingTools;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObject;

namespace Mapping_Tools.Desktop.Interactions.GeometryDashboard;

/// <summary>Edits one neutral geometry appearance group.</summary>
public sealed class GeometryDashboardPreferenceRowViewModel : ObservableObject
{
    private string? pendingColorText;

    /// <summary>Creates a row over one cloned appearance group.</summary>
    public GeometryDashboardPreferenceRowViewModel(RelevantObjectPreferences preference)
    {
        Preference = preference;
    }

    /// <summary>Gets the stable preference-group label.</summary>
    public string Name => Preference.Name;

    /// <summary>Gets the Core appearance settings.</summary>
    public RelevantObjectPreferences Preference { get; }

    /// <summary>Gets or sets the color using Avalonia's color-picker type.</summary>
    public Color Color
    {
        get => Color.FromArgb(Preference.Color.A, Preference.Color.R, Preference.Color.G, Preference.Color.B);
        set
        {
            Preference.Color = RgbaColour.FromArgb(value.A, value.R, value.G, value.B);
            ColorTextError = null;
            pendingColorText = null;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ColorText));
            OnPropertyChanged(nameof(ColorTextError));
        }
    }

    /// <summary>Gets or sets the serialized color text.</summary>
    public string ColorText
    {
        get => pendingColorText ?? Preference.Color.ToString();
        set
        {
            string hex = (value ?? string.Empty).TrimStart('#');
            if (hex.Length == 6) hex = "FF" + hex;
            if (hex.Length == 8
                && byte.TryParse(hex[..2], NumberStyles.HexNumber, null, out byte a)
                && byte.TryParse(hex[2..4], NumberStyles.HexNumber, null, out byte r)
                && byte.TryParse(hex[4..6], NumberStyles.HexNumber, null, out byte g)
                && byte.TryParse(hex[6..8], NumberStyles.HexNumber, null, out byte b))
            {
                Preference.Color = RgbaColour.FromArgb(a, r, g, b);
                ColorTextError = null;
                pendingColorText = null;
            }
            else
            {
                ColorTextError = "Color format error.";
                pendingColorText = value;
            }

            OnPropertyChanged(nameof(Color));
            OnPropertyChanged();
            OnPropertyChanged(nameof(ColorTextError));
        }
    }

    /// <summary>Gets the validation message for invalid hexadecimal colour text.</summary>
    public string? ColorTextError { get; private set; }

    /// <summary>Gets or sets the opacity multiplier.</summary>
    public double Opacity
    {
        get => Preference.Opacity;
        set
        {
            Preference.Opacity = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Gets or sets the stroke thickness.</summary>
    public double Thickness
    {
        get => Preference.Thickness;
        set
        {
            Preference.Thickness = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Gets or sets the point size where supported.</summary>
    public double Size
    {
        get => Preference.Size;
        set
        {
            Preference.Size = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Gets whether point size applies to this group.</summary>
    public bool HasSizeOption => Preference.HasSizeOption;

    /// <summary>Gets or sets the dash pattern.</summary>
    public DashStylesEnum DashStyle
    {
        get => Preference.Dashstyle;
        set
        {
            Preference.Dashstyle = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Gets all available dash patterns.</summary>
    public IReadOnlyList<DashStylesEnum> DashStyles { get; } = Enum.GetValues<DashStylesEnum>();
}

