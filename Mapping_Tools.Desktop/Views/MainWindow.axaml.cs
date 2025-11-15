using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Mapping_Tools.Desktop.ViewModels;
using Material.Icons;
using Material.Icons.Avalonia;
using Material.Styles.Controls;
using Material.Styles.Models;
using ReactiveUI;

namespace Mapping_Tools.Desktop.Views;

public partial class MainWindow : Window {
    private bool autoSave = true;
    private bool updateAfterClose;
    private Task downloadUpdateTask;

    private MainWindowViewModel ViewModel => (MainWindowViewModel) DataContext;

    public static MainWindow AppWindow { get; set; }
    public static string AppCommon { get; set; }
    public static string AppDataPath { get; set; }
    public static string ExportPath { get; set; }
    public static HttpClient HttpClient { get; set; }

    public delegate void CurrentBeatmapUpdateHandler(object sender, string currentBeatmaps);

    public event CurrentBeatmapUpdateHandler OnUpdateCurrentBeatmap;

    public void UpdateCurrentBeatmap(string currentBeatmaps) {
        // Make sure someone is listening to event
        if (OnUpdateCurrentBeatmap == null) return;
        OnUpdateCurrentBeatmap(this, currentBeatmaps);
    }

    public MainWindow() {
        InitializeComponent();

        PropertyChanged += (_, e) => {
            if (e.Property == WindowStateProperty) {
                Window_StateChanged();
            }
        };

        AddHandler(DragDrop.DragEnterEvent, MainWindow_DragEnter);
        AddHandler(DragDrop.DropEvent, MainWindow_Drop);

        this.GetObservable(DataContextProperty).Subscribe(dc => {
            if (dc is MainWindowViewModel vm)
                vm.WhenAnyValue(x => x.IsBusy).Subscribe(busy =>
                    Cursor = busy ? new Cursor(StandardCursorType.Wait) : null); // null = inherit/default
        });


        AppWindow = this;
        AppCommon = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        AppDataPath = Path.Combine(AppCommon, "Mapping Tools");
        ExportPath = Path.Combine(AppDataPath, "Exports");
        HttpClient = new HttpClient();
        HttpClient.DefaultRequestHeaders.Add("user-agent", "Mapping Tools");

        InitializeComponent();

        // if (SettingsManager.Settings.MainWindowRestoreBounds.HasValue) {
        // SetToRect(SettingsManager.Settings.MainWindowRestoreBounds.Value);
        // }

        // SetFullscreen(SettingsManager.Settings.MainWindowMaximized);

        // SetCurrentMaps(SettingsManager.GetLatestCurrentMaps()); // Set currentmap to previously opened map
    }

    //Close window
    private void CloseWin(object sender, RoutedEventArgs e) {
        Close();
    }

    //Close window without saving
    private void CloseWinNoSave(object sender, RoutedEventArgs e) {
        autoSave = false;
        Close();
    }

    public void SetCurrentMaps(string[] paths) {
        string strPaths = string.Join("|", paths);
        ViewModel.CurrentBeatmaps = strPaths;
        UpdateCurrentBeatmap(strPaths);
        // SettingsManager.AddRecentMap(paths, DateTime.Now);
    }

    public void SetCurrentMapsString(string paths) {
        ViewModel.CurrentBeatmaps = paths;
        UpdateCurrentBeatmap(paths);
        // SettingsManager.AddRecentMap(paths.Split('|'), DateTime.Now);
    }

    public string[] GetCurrentMaps() {
        var maps = ViewModel.CurrentBeatmaps.Split('|');

        if (maps.Any(o => !File.Exists(o))) {
            var model = new SnackbarModel("It seems like one of the selected beatmaps does not exist. Please re-select the file with 'File > Open beatmap'.",
                TimeSpan.FromSeconds(10));
            SnackbarHost.Post(model, "MainSnackbar", DispatcherPriority.Normal);
        }

        return maps;
    }

    public string GetCurrentMapsString() {
        return string.Join("|", GetCurrentMaps());
    }

    private void OpenBeatmap(object sender, RoutedEventArgs e) {
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

    private async void OpenGetCurrentBeatmap(object sender, RoutedEventArgs e) {
        // try {
        //     string path = await Task.Run(() => IOHelper.GetCurrentBeatmap());
        //     if (path != "") {
        //         SetCurrentMaps(new[] { path });
        //     }
        // } catch (Exception ex) {
        //     ex.Show();
        // }
    }

    private async void SaveBackup(object sender, RoutedEventArgs e) {
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

    private async void LoadBackup(object sender, RoutedEventArgs e) {
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
    private void OpenBackups(object sender, RoutedEventArgs e) {
        // try {
        //     System.Diagnostics.Process.Start("explorer.exe", SettingsManager.GetBackupsPath());
        // }
        // catch( Exception ex ) {
        //     ex.Show();
        // }
    }

    private void OpenConfig(object sender, RoutedEventArgs e) {
        // try {
        //     System.Diagnostics.Process.Start("explorer.exe", AppDataPath);
        // }
        // catch( Exception ex ) {
        //     ex.Show();
        // }
    }

    private void OpenWebsite(object sender, RoutedEventArgs e) {
        Launcher.LaunchUriAsync(new Uri("https://mappingtools.github.io")).GetAwaiter().GetResult();
    }

    private async void CoolSave(object sender, RoutedEventArgs e) {
        // try {
        //     await Task.Run(() => EditorReaderStuff.BetterSave());
        // } catch (Exception ex) {
        //     ex.Show();
        // }
    }

    //Open project in browser
    private void OpenGitHub(object sender, RoutedEventArgs e) {
        Launcher.LaunchUriAsync(new Uri("https://github.com/OliBomby/Mapping_Tools")).GetAwaiter().GetResult();
    }

    //Open project in browser
    private void OpenDonate(object sender, RoutedEventArgs e) {
        Launcher.LaunchUriAsync(new Uri("https://ko-fi.com/olibomby")).GetAwaiter().GetResult();
    }

    //Open info screen
    private void OpenInfo(object sender, RoutedEventArgs e) {
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

    //Change top right icons on changed window state and set state variable
    private void Window_StateChanged() {
        switch (WindowState) {
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

    //Clickevent for top right maximize/minimize button
    private void ToggleWin(object sender, RoutedEventArgs e) {
        SetFullscreen(WindowState != WindowState.Maximized);
    }

    private void SetFullscreen(bool fullscreen, bool actuallyChangeFullscreen = true) {
        if (fullscreen) {
            if (actuallyChangeFullscreen) {
                WindowState = WindowState.Maximized;
            }

            ToggleButton.Content = new MaterialIcon { Kind = MaterialIconKind.WindowRestore };
        } else {
            if (actuallyChangeFullscreen) {
                WindowState = WindowState.Normal;
            }

            ToggleButton.Content = new MaterialIcon { Kind = MaterialIconKind.WindowMaximize };
        }

        EnsureOnScreen();
    }

    private void SetToRect(PixelRect rect) {
        if (Screens.ScreenCount == 0)
            return;

        var screenBounds = Screens.All.Aggregate(Screens.Primary!.Bounds, (current, screen) => current.Union(screen.Bounds));

        Position = new PixelPoint(
            Math.Clamp(rect.X, screenBounds.X, screenBounds.Right - 300),
            Math.Clamp(rect.Y, screenBounds.Y, screenBounds.Bottom - 100)
        );
        Width = Math.Max(rect.Width, 300);
        Height = Math.Max(rect.Height, 100);
    }

    private void EnsureOnScreen() {
        SetToRect(new PixelRect(Position.X, Position.Y, (int) Width, (int) Height));
    }

    //Minimize window on click
    private void MinimizeWin(object sender, RoutedEventArgs e) {
        WindowState = WindowState.Minimized;
    }

    //Enable drag control of window and set icons when docked
    private void DragWin(object sender, PointerPressedEventArgs e) {
        if (!e.Properties.IsLeftButtonPressed)
            return;

        if (WindowState == WindowState.Maximized) {
            SetFullscreen(false);
        }

        BeginMoveDrag(e);

        e.Handled = true;
    }

    private void CheckForUpdates(object sender, RoutedEventArgs e) {
        // await Update(false, true);
    }

    private void MainWindow_DragEnter(object? sender, DragEventArgs e) {
        // Check if the dragged data is a file
        e.DragEffects = e.DataTransfer.Formats.Any(o => o.Equals(DataFormat.File)) ? DragDropEffects.Copy : DragDropEffects.None;
    }

    private void MainWindow_Drop(object? sender, DragEventArgs e) {
        // Get the array of file paths
        if (e.DataTransfer.Items[0].TryGetRaw(DataFormat.File) is string[] { Length: > 0 } files)
            SetCurrentMaps(files);
    }
}