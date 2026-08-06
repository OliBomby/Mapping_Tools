using Mapping_Tools.Desktop.ViewModels;

namespace Mapping_Tools.Desktop.Interactions;

/// <summary>Shows the reusable modeless Rhythm Guide surface without view-model window construction.</summary>
public interface IRhythmGuideWindowService
{
    void Show(RhythmGuideViewModel viewModel);
}
