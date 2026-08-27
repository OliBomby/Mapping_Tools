using Mapping_Tools.Desktop.ViewModels;
using Mapping_Tools.Desktop.Tools.RhythmGuide.ViewModels;

namespace Mapping_Tools.Desktop.Tools.RhythmGuide.Interactions;

/// <summary>Shows the reusable modeless Rhythm Guide surface without view-model window construction.</summary>
public interface IRhythmGuideWindowService
{
    /// <summary>Shows or activates a modeless window bound to the supplied project model.</summary>
    /// <param name="viewModel">The shared Rhythm Guide project state.</param>
    void Show(RhythmGuideViewModel viewModel);
}
