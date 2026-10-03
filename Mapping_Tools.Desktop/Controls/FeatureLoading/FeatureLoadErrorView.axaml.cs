using Avalonia;
using Avalonia.Controls;

namespace Mapping_Tools.Desktop.Controls.FeatureLoading;

/// <summary>Shows a recoverable error when a feature cannot be constructed or presented.</summary>
public sealed partial class FeatureLoadErrorView : UserControl
{
    /// <summary>Identifies the error message shown to the user.</summary>
    public static readonly StyledProperty<string?> MessageProperty =
        AvaloniaProperty.Register<FeatureLoadErrorView, string?>(nameof(Message));

    /// <summary>Identifies the original diagnostic details for the loading failure.</summary>
    public static readonly StyledProperty<string?> DetailsProperty =
        AvaloniaProperty.Register<FeatureLoadErrorView, string?>(nameof(Details));

    /// <summary>Creates the feature loading error view.</summary>
    public FeatureLoadErrorView()
    {
        InitializeComponent();
    }

    /// <summary>Gets or sets the user-facing loading error.</summary>
    public string? Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    /// <summary>Gets or sets the untranslated exception details, available in a collapsible expander.</summary>
    public string? Details
    {
        get => GetValue(DetailsProperty);
        set => SetValue(DetailsProperty, value);
    }
}
