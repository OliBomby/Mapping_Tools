using System.Reactive;
using System.Threading.Tasks;
using Mapping_Tools.Application;
using ReactiveUI;

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
    
    private bool isBusy;

    public bool IsBusy
    {
        get => isBusy;
        private set => this.RaiseAndSetIfChanged(ref isBusy, value);
    }

    public ReactiveCommand<Unit, Unit>? GoHomeCommand { get; }
    public ReactiveCommand<Unit, Unit>? GoSettingsCommand { get; }
    
    public MainWindowViewModel() : this(null!, null!) { }

    public MainWindowViewModel(NavigationService navigationService, IAppLifecycle appLifecycle) {
        this.navigationService = navigationService;
        this.navigationService.OnNavigate += vm => CurrentViewModel = vm;
        
        // Ensure current view model is disposed on app exit
        appLifecycle.UICleanup.Register(() => navigationService.DisposeCurrentAsync(CurrentViewModel).GetAwaiter().GetResult());
        
        Task.Run(() => this.navigationService.NavigateAsync<HomeViewModel>());
        
        GoHomeCommand = ReactiveCommand.CreateFromTask(NavigateAsync<HomeViewModel>);
        GoSettingsCommand = ReactiveCommand.CreateFromTask(NavigateAsync<SettingsViewModel>);
    }
    
    private async Task NavigateAsync<T>() where T : ViewModelBase {
        IsBusy = true;
        await navigationService.NavigateAsync<T>();
        IsBusy = false;
    }
}