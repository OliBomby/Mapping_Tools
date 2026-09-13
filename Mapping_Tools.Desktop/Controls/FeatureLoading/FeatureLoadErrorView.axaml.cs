using Avalonia;
using Avalonia.Controls;

namespace Mapping_Tools.Desktop.Controls.FeatureLoading;

/// <summary>Shows a recoverable error when a feature cannot be constructed or presented.</summary>
public sealed partial class FeatureLoadErrorView : UserControl
{
    /// <summary>Identifies the error message shown to the user.</summary>
    public static readonly StyledProperty<string?> MessageProperty =
        AvaloniaProperty.Register<FeatureLoadErrorView, string?>(nameof(Message));

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
}
