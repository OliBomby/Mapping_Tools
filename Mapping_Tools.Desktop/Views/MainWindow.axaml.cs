using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Mapping_Tools.Desktop.ViewModels;
using Material.Icons;
using Material.Icons.Avalonia;
using ReactiveUI;

namespace Mapping_Tools.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        PropertyChanged += (_, e) =>
        {
            if (e.Property == WindowStateProperty)
            {
                Window_StateChanged();
            }
        };

        AddHandler(DragDrop.DragEnterEvent, MainWindow_DragEnter);
        AddHandler(DragDrop.DropEvent, MainWindow_Drop);

        // Handle window position restoration
        this.GetObservable(DataContextProperty).Subscribe(dc =>
        {
            if (dc is not MainWindowViewModel vm)
                return;

            vm.WhenAnyValue(x => x.IsBusy).Subscribe(busy =>
                Cursor = busy ? new Cursor(StandardCursorType.Wait) : null); // null = inherit/default
            
            if (vm.UserSettings.MainWindowRestoreBounds is not null)
                SetPositionRect(vm.UserSettings.MainWindowRestoreBounds);
        });

        // SetFullscreen(SettingsManager.Settings.MainWindowMaximized);

        // SetCurrentMaps(SettingsManager.GetLatestCurrentMaps()); // Set currentmap to previously opened map
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        
        if (DataContext is MainWindowViewModel vm) 
            vm.UserSettings.MainWindowRestoreBounds = GetPositionRect();
    }

    //Close window
    private void CloseWin(object sender, RoutedEventArgs e)
    {
        Close();
    }

    //Close window without saving
    private void CloseWinNoSave(object sender, RoutedEventArgs e)
    {
        // autoSave = false;
        Close();
    }

    private void OpenBeatmap(object sender, RoutedEventArgs e)
    {
        // try {
        //     string[] paths = IOHelper.BeatmapFileDialog(true);
        //     if( paths.Length != 0 ) {
        //         SetCurrentMaps(paths);
        //     }
        // }
        // catch( Exception ex ) {
        //     ex.Show();
        // }
    }

    private async void OpenGetCurrentBeatmap(object sender, RoutedEventArgs e)
    {
        // try {
        //     string path = await Task.Run(() => IOHelper.GetCurrentBeatmap());
        //     if (path != "") {
        //         SetCurrentMaps(new[] { path });
        //     }
        // } catch (Exception ex) {
        //     ex.Show();
        // }
    }

    private async void SaveBackup(object sender, RoutedEventArgs e)
    {
        // try {
        //     var paths = GetCurrentMaps();
        //     var result = await Task.Run(() => BackupManager.SaveMapBackup(paths, true, "UB"));  // UB stands for User Backup
        //     if (result) {
        //         await Task.Run(() => MessageQueue.Enqueue($"Beatmap{( paths.Length == 1 ? "" : "s" )} successfully copied!"));
        //     }
        // } catch (Exception ex) {
        //     ex.Show();
        // }
    }

    private async void LoadBackup(object sender, RoutedEventArgs e)
    {
        // try {
        //     var paths = GetCurrentMaps();
        //     if (paths.Length > 1) {
        //         throw new Exception($"Can't load backup into multiple beatmaps. You currently have {paths.Length} beatmaps selected.");
        //     }
        //     var backupPaths = IOHelper.BeatmapFileDialog(SettingsManager.GetBackupsPath(), false);
        //     if (backupPaths.Length == 1) {
        //         try {
        //             await Task.Run(() => BackupManager.LoadMapBackup(backupPaths[0], paths[0], false));
        //         } catch (BeatmapIncompatibleException ex) {
        //             var exResult = ex.Show();
        //             if (exResult == MessageBoxResult.Cancel) {
        //                 return;
        //             }
        //
        //             var result = MessageBox.Show("Do you want to load the backup anyways?", "Load backup",
        //                 MessageBoxButton.YesNo);
        //             if (result == MessageBoxResult.Yes) {
        //                 await Task.Run(() => BackupManager.LoadMapBackup(backupPaths[0], paths[0], true));
        //             } else {
        //                 return;
        //             }
        //         }
        //         await Task.Run(() => MessageQueue.Enqueue("Backup successfully loaded!"));
        //     }
        // } catch (Exception ex) {
        //     ex.Show();
        // }
    }

    //Open backup folder in file explorer
    private void OpenBackups(object sender, RoutedEventArgs e)
    {
        // try {
        //     System.Diagnostics.Process.Start("explorer.exe", SettingsManager.GetBackupsPath());
        // }
        // catch( Exception ex ) {
        //     ex.Show();
        // }
    }

    private void OpenConfig(object sender, RoutedEventArgs e)
    {
        // try {
        //     System.Diagnostics.Process.Start("explorer.exe", AppDataPath);
        // }
        // catch( Exception ex ) {
        //     ex.Show();
        // }
    }

    private void OpenWebsite(object sender, RoutedEventArgs e)
    {
        Launcher.LaunchUriAsync(new Uri("https://mappingtools.github.io")).GetAwaiter().GetResult();
    }

    private async void CoolSave(object sender, RoutedEventArgs e)
    {
        // try {
        //     await Task.Run(() => EditorReaderStuff.BetterSave());
        // } catch (Exception ex) {
        //     ex.Show();
        // }
    }

    //Open project in browser
    private void OpenGitHub(object sender, RoutedEventArgs e)
    {
        Launcher.LaunchUriAsync(new Uri("https://github.com/OliBomby/Mapping_Tools")).GetAwaiter().GetResult();
    }

    //Open project in browser
    private void OpenDonate(object sender, RoutedEventArgs e)
    {
        Launcher.LaunchUriAsync(new Uri("https://ko-fi.com/olibomby")).GetAwaiter().GetResult();
    }

    //Open info screen
    private void OpenInfo(object sender, RoutedEventArgs e)
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version;
        var builder = new StringBuilder();
        builder.AppendLine($"Mapping Tools {version}");
        builder.AppendLine();
        builder.AppendLine("Made by:");
        builder.AppendLine("OliBomby");
        builder.AppendLine();
        builder.AppendLine("Supporters:");
        builder.AppendLine("Mercury");
        builder.AppendLine("Ryuusei Aika");
        builder.AppendLine("Pon -");
        builder.AppendLine("Spoppyboi");
        builder.AppendLine("fanzhen0019");
        builder.AppendLine("spon");
        builder.AppendLine("Joshua Saku");
        builder.AppendLine("Julaaaan");
        builder.AppendLine("pizzafanboy");
        builder.AppendLine("ZEduards");
        builder.AppendLine("Dcs");
        builder.AppendLine();
        builder.AppendLine("Contributors:");
        builder.AppendLine("Potoofu");
        builder.AppendLine("Karoo13");
        builder.AppendLine("Coppertine");
        builder.AppendLine("JPK314");

        MessageBox.Show(this, builder.ToString(), "Info");
    }

    private void Window_StateChanged()
    {
        switch (WindowState)
        {
            case WindowState.Maximized:
                SetFullscreen(true, false);
                break;

            case WindowState.Minimized:
                break;

            case WindowState.Normal:
                SetFullscreen(false, false);
                break;
        }
    }

    private void ToggleWin(object sender, RoutedEventArgs e)
    {
        SetFullscreen(WindowState != WindowState.Maximized);
    }

    private void SetFullscreen(bool fullscreen, bool actuallyChangeFullscreen = true)
    {
        if (fullscreen)
        {
            if (actuallyChangeFullscreen)
            {
                WindowState = WindowState.Maximized;
            }

            ToggleButton.Content = new MaterialIcon { Kind = MaterialIconKind.WindowRestore };
        } else
        {
            if (actuallyChangeFullscreen)
            {
                WindowState = WindowState.Normal;
            }

            ToggleButton.Content = new MaterialIcon { Kind = MaterialIconKind.WindowMaximize };
        }
    }

    private void SetPositionRect(int[] rect)
    {
        if (Screens.ScreenCount == 0)
            return;

        var screenBounds = Screens.All.Aggregate(Screens.Primary!.Bounds, (current, screen) => current.Union(screen.Bounds));

        Position = new PixelPoint(
            Math.Clamp(rect[0], screenBounds.X, screenBounds.Right - 300),
            Math.Clamp(rect[1], screenBounds.Y, screenBounds.Bottom - 100)
        );
        Width = Math.Max(rect[2], 300);
        Height = Math.Max(rect[3], 100);
    }
    
    private int[] GetPositionRect()
    {
        return [Position.X, Position.Y, (int)Width, (int)Height];
    }

    private void MinimizeWin(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void DragWin(object sender, PointerPressedEventArgs e)
    {
        if (!e.Properties.IsLeftButtonPressed)
            return;

        if (WindowState == WindowState.Maximized)
            SetFullscreen(false);

        BeginMoveDrag(e);

        e.Handled = true;
    }

    private void MainWindow_DragEnter(object? sender, DragEventArgs e)
    {
        // Check if the dragged data is a file
        e.DragEffects = e.DataTransfer.Formats.Any(o => o.Equals(DataFormat.File)) ? DragDropEffects.Copy : DragDropEffects.None;
    }

    private void MainWindow_Drop(object? sender, DragEventArgs e)
    {
        // Get the array of file paths
        if (e.DataTransfer.Items[0].TryGetRaw(DataFormat.File) is string[] { Length: > 0 } files)
            (DataContext as MainWindowViewModel)?.SetCurrentBeatmaps(files);
    }

    private void ToolsMenu_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        // Find the ListBoxItem that was actually clicked
        var source = e.Source as Control;
        var container = source?.FindAncestorOfType<ListBoxItem>();

        // Get the command you bound via Tag
        if (container?.Tag is ICommand cmd && cmd.CanExecute(null))
        {
            cmd.Execute(null);
            e.Handled = true;
        }
    }

    private void CheckForUpdates(object? sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }
}