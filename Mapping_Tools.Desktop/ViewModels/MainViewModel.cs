using ReactiveUI;

namespace Mapping_Tools.Desktop.ViewModels;

/// <summary>
/// Supplies the presentation state for the temporary Avalonia migration shell.
/// </summary>
public class MainViewModel : ViewModelBase
{
    private string _greeting = "Mapping Tools — Avalonia migration shell";

    /// <summary>
    /// Gets or sets the status text displayed by the shell.
    /// </summary>
    public string Greeting
    {
        get => _greeting;
        set => this.RaiseAndSetIfChanged(ref _greeting, value);
    }
}
