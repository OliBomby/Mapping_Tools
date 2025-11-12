using System.Reactive;
using System.Threading.Tasks;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace Mapping_Tools.Desktop.ViewModels;

public class MainWindowViewModel : ViewModelBase {
    private NavigationService navigationService;
    private ViewModelBase? currentViewModel;

    public ViewModelBase? CurrentViewModel {
        get => currentViewModel;
        set
        {
            // Capture local variable to prevent currentViewModel from changing before DisposeCurrentAsync is called
            var previousViewModel = currentViewModel;
            Task.Run(() => navigationService.DisposeCurrentAsync(previousViewModel));
            
            this.RaiseAndSetIfChanged(ref currentViewModel, value);
        }
    }

    [Reactive] public string? Note { get; set; }

    public ReactiveCommand<Unit, Unit>? GoHomeCommand { get; }
    public ReactiveCommand<Unit, Unit>? GoSettingsCommand { get; }

    public MainWindowViewModel(NavigationService navigationService) {
        this.navigationService = navigationService;
        
        this.navigationService.OnNavigate += vm => CurrentViewModel = vm;
        Task.Run(() => this.navigationService.NavigateAsync<HomeViewModel>());
        
        GoHomeCommand = ReactiveCommand.CreateFromTask(navigationService.NavigateAsync<HomeViewModel>);
        GoSettingsCommand = ReactiveCommand.CreateFromTask(navigationService.NavigateAsync<SettingsViewModel>);
    }
}