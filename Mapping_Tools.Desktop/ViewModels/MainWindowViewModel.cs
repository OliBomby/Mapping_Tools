using System;
using System.Reactive;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace Mapping_Tools.Desktop.ViewModels;

public class MainWindowViewModel : ViewModelBase {
    private ViewModelBase? currentViewModel;

    public ViewModelBase? CurrentViewModel {
        get => currentViewModel;
        set => this.RaiseAndSetIfChanged(ref currentViewModel, value);
    }

    public ReactiveCommand<Unit, ViewModelBase> GoHomeCommand { get; }
    public ReactiveCommand<Unit, ViewModelBase> GoSettingsCommand { get; }

    public MainWindowViewModel(IServiceProvider serviceProvider) {
        currentViewModel = serviceProvider.GetRequiredService<HomeViewModel>();

        GoHomeCommand = ReactiveCommand.Create(
            () => CurrentViewModel = serviceProvider.GetRequiredService<HomeViewModel>(),
            outputScheduler: RxApp.MainThreadScheduler
        );

        GoSettingsCommand = ReactiveCommand.Create(
            () => CurrentViewModel = serviceProvider.GetRequiredService<SettingsViewModel>(),
            outputScheduler: RxApp.MainThreadScheduler
        );
    }
}