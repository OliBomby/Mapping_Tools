using Avalonia;
using Avalonia.Controls;

namespace Mapping_Tools.Desktop.Controls;

/// <summary>
///     Presents the consistent title, help affordance, and optional QuickRun badge used by mapping tools.
/// </summary>
public sealed partial class ToolViewHeader : UserControl
{
    /// <summary>Identifies the title displayed in the tool header.</summary>
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<ToolViewHeader, string>(nameof(Title), string.Empty);

    /// <summary>Identifies the explanatory text displayed by the help flyout.</summary>
    public static readonly StyledProperty<string> HelpTextProperty =
        AvaloniaProperty.Register<ToolViewHeader, string>(nameof(HelpText), string.Empty);

    /// <summary>Identifies whether the tool header displays the QuickRun information badge.</summary>
    public static readonly StyledProperty<bool> IsQuickRunSupportedProperty =
        AvaloniaProperty.Register<ToolViewHeader, bool>(nameof(IsQuickRunSupported));

    /// <summary>Creates a tool header and loads its shared visual structure.</summary>
    public ToolViewHeader()
    {
        InitializeComponent();
    }

    /// <summary>Gets or sets the tool name shown as the page heading.</summary>
    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>Gets or sets the longer explanation opened from the help badge.</summary>
    public string HelpText
    {
        get => GetValue(HelpTextProperty);
        set => SetValue(HelpTextProperty, value);
    }

    /// <summary>Gets or sets whether the tool supports the global QuickRun workflow.</summary>
    public bool IsQuickRunSupported
    {
        get => GetValue(IsQuickRunSupportedProperty);
        set => SetValue(IsQuickRunSupportedProperty, value);
    }
}
