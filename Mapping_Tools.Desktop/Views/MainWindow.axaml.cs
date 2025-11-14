using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Mapping_Tools.Desktop.ViewModels;
using ReactiveUI;

namespace Mapping_Tools.Desktop.Views;

public partial class MainWindow : Window {
    public MainWindow() {
        InitializeComponent();
        
        this.GetObservable(DataContextProperty).Subscribe(dc =>
        {
            if (dc is MainWindowViewModel vm)
                vm.WhenAnyValue(x => x.IsBusy).Subscribe(busy =>
                    Cursor = busy ? new Cursor(StandardCursorType.Wait) : null); // null = inherit/default
        });
    }
    
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        if (XpfWpfAbstraction.IsRunningOnXpf)
        {
            if (XpfWpfAbstraction.GetAvaloniaWindowForWindow(this) is { } window)
            {
                window.ExtendClientAreaToDecorationsHint = true;
                // window.ExtendClientAreaChromeHints = Avalonia.Platform.ExtendClientAreaChromeHints.NoChrome;
            }

        }
    }
}