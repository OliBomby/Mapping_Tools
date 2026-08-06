using Mapping_Tools.Desktop.ViewModels;
using Mapping_Tools.Desktop.Views;

namespace Mapping_Tools.Desktop.Interactions;

internal sealed class AvaloniaRhythmGuideWindowService : IRhythmGuideWindowService
{
    private readonly Func<MainWindow> _owner;
    private readonly Dictionary<RhythmGuideViewModel, RhythmGuideWindow> _windows = [];

    public AvaloniaRhythmGuideWindowService(Func<MainWindow> owner)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public void Show(RhythmGuideViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        if (_windows.TryGetValue(viewModel, out RhythmGuideWindow? existing))
        {
            existing.Activate();
            return;
        }

        RhythmGuideWindow window = new() { DataContext = viewModel };
        _windows.Add(viewModel, window);
        window.Closed += (_, _) => _windows.Remove(viewModel);
        window.Show(_owner());
    }
}
