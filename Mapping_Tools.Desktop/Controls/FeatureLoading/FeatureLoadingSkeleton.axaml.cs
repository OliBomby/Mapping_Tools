using Avalonia.Controls;

namespace Mapping_Tools.Desktop.Controls.FeatureLoading;

/// <summary>Shows the lightweight placeholder displayed while a feature is constructed.</summary>
public sealed partial class FeatureLoadingSkeleton : UserControl
{
    /// <summary>Creates the feature loading placeholder.</summary>
    public FeatureLoadingSkeleton()
    {
        InitializeComponent();
    }
}
