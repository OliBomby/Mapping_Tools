using Mapping_Tools.Desktop.Services.Undo;
using System.Globalization;
using Avalonia.Media;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.Tools.GeometryDashboard;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObject;
using Mapping_Tools.Desktop.Localization;

namespace Mapping_Tools.Desktop.Tools.GeometryDashboard.ViewModels;

/// <summary>Edits one neutral geometry appearance group.</summary>
public sealed class GeometryDashboardPreferenceRowViewModel : LocalizedObservableObject
{
    private readonly string originalName;
    private string? pendingColorText;
    private bool hasColorTextError;

    /// <summary>Creates a row over one cloned appearance group.</summary>
    /// <param name="name">The stable preference-group label.</param>
    /// <param name="preference">The cloned Core appearance settings.</param>
    /// <param name="hasSizeOption">Whether this row exposes the point-size editor.</param>
    public GeometryDashboardPreferenceRowViewModel(
        string name,
        RelevantObjectPreferences preference,
        bool hasSizeOption)
    {
        originalName = name;
        Preference = preference;
        HasSizeOption = hasSizeOption;
    }

    /// <summary>Gets the stable preference-group label.</summary>
    public string Name => originalName switch
    {
        "Virtual point preferences" => DesktopStrings.GeometryDashboard_AppearancePoint,
        "Virtual line preferences" => DesktopStrings.GeometryDashboard_AppearanceLine,
        "Virtual circle preferences" => DesktopStrings.GeometryDashboard_AppearanceCircle,
        _ => originalName,
    };

    /// <summary>Gets the Core appearance settings.</summary>
    [Undoable]
    public RelevantObjectPreferences Preference { get; }

    /// <summary>Gets or sets the color using Avalonia's color-picker type.</summary>
    public Color Color
    {
        get => Color.FromArgb(Preference.Color.A, Preference.Color.R, Preference.Color.G, Preference.Color.B);
        set
        {
            Preference.Color = RgbaColour.FromArgb(value.A, value.R, value.G, value.B);
            hasColorTextError = false;
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
            string hex = value.TrimStart('#');
            if (hex.Length == 6) hex = "FF" + hex;
            if (hex.Length == 8
                && byte.TryParse(hex[..2], NumberStyles.HexNumber, null, out byte a)
                && byte.TryParse(hex[2..4], NumberStyles.HexNumber, null, out byte r)
                && byte.TryParse(hex[4..6], NumberStyles.HexNumber, null, out byte g)
                && byte.TryParse(hex[6..8], NumberStyles.HexNumber, null, out byte b))
            {
                Preference.Color = RgbaColour.FromArgb(a, r, g, b);
                hasColorTextError = false;
                pendingColorText = null;
            }
            else
            {
                hasColorTextError = true;
                pendingColorText = value;
            }

            OnPropertyChanged(nameof(Color));
            OnPropertyChanged();
            OnPropertyChanged(nameof(ColorTextError));
        }
    }

    /// <summary>Gets the validation message for invalid hexadecimal colour text.</summary>
    public string? ColorTextError => hasColorTextError ? DesktopStrings.GeometryDashboard_ColorFormatError : null;

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
    public bool HasSizeOption { get; }

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
