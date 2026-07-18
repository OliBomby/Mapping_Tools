using ReactiveUI;

namespace Mapping_Tools.Desktop.ViewModels;

public class MainViewModel : ViewModelBase
{
    private string _greeting = "Mapping Tools — Avalonia migration shell";

    public string Greeting
    {
        get => _greeting;
        set => this.RaiseAndSetIfChanged(ref _greeting, value);
    }
}
