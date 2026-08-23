using Mapping_Tools.Desktop.ViewModels;
using Mapping_Tools.Desktop.Views;

namespace Mapping_Tools.Desktop.Interactions;

internal sealed class AvaloniaRhythmGuideWindowService : IRhythmGuideWindowService
{
    private readonly Func<MainWindow> owner;
    private readonly Dictionary<RhythmGuideViewModel, RhythmGuideWindow> windows = [];

    public AvaloniaRhythmGuideWindowService(Func<MainWindow> owner)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public void Show(RhythmGuideViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        if (windows.TryGetValue(viewModel, out var existing))
        {
            existing.Activate();
            return;
        }

        RhythmGuideWindow window = new() { DataContext = viewModel };
        windows.Add(viewModel, window);
        window.Closed += (_, _) => windows.Remove(viewModel);
        window.Show(owner());
    }
}
