﻿using Mapping_Tools.Classes;
using Mapping_Tools.Classes.Exceptions;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.ToolHelpers;
using Mapping_Tools.Updater;
using Mapping_Tools.Viewmodels;
using Mapping_Tools.Views;
using MaterialDesignThemes.Wpf;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools {

    public partial class MainWindow {
        private bool autoSave = true;
        private UpdaterWindow updaterWindow;
        private bool updateAfterClose;
        private Task downloadUpdateTask;
        private UpdateManager updateManager;

        private MainWindowVm ViewModel => (MainWindowVm)DataContext;

        public ListenerManager ListenerManager;
        public bool SessionhasAdminRights;
        public ViewCollection Views => ViewModel.Views;

        public static MainWindow AppWindow { get; set; }
        public static string AppCommon { get; set; }
        public static string AppDataPath { get; set; }
        public static string ExportPath { get; set; }
        public static HttpClient HttpClient { get; set; }
        public static SnackbarMessageQueue MessageQueue { get; set; }

        public delegate void CurrentBeatmapUpdateHandler(object sender, string currentBeatmaps);

        public event CurrentBeatmapUpdateHandler OnUpdateCurrentBeatmap;
        public void UpdateCurrentBeatmap(string currentBeatmaps)
        {
            // Make sure someone is listening to event
            if (OnUpdateCurrentBeatmap == null) return;
            OnUpdateCurrentBeatmap(this, currentBeatmaps);
        }

        public MainWindow() {
            try {
                AppWindow = this;
                AppCommon = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                AppDataPath = Path.Combine(AppCommon, "Mapping Tools");
                ExportPath = Path.Combine(AppDataPath, "Exports");
                HttpClient = new HttpClient();
                HttpClient.DefaultRequestHeaders.Add("user-agent", "Mapping Tools");

                InitializeComponent();

                Setup();
                SettingsManager.LoadConfig();
                ListenerManager = new ListenerManager();

                DataContext = new MainWindowVm();

                MessageQueue = new SnackbarMessageQueue(TimeSpan.FromSeconds(2));
                MainSnackbar.MessageQueue = MessageQueue;

                if (SettingsManager.Settings.MainWindowRestoreBounds.HasValue) {
                    SetToRect(SettingsManager.Settings.MainWindowRestoreBounds.Value);
                }

                SetFullscreen(SettingsManager.Settings.MainWindowMaximized);

                SetCurrentMaps(SettingsManager.GetLatestCurrentMaps()); // Set currentmap to previously opened map
            }
            catch( Exception ex ) {
                ex.Show();
            }
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e) {
            _ = Update();
        }

        private void Setup() {
            SessionhasAdminRights = IsUserAdministrator();
            
            try {
                Directory.CreateDirectory(AppDataPath);
                Directory.CreateDirectory(ExportPath);
            }

            catch( Exception ex ) {
                ex.Show();
            }
        }

        private async Task Update(bool allowSkip = true, bool notifyUser = false) {
            try {
                var assetNamePattern = Environment.Is64BitProcess ? "release_x64.zip" : "release.zip";
                updateManager = new UpdateManager("OliBomby", "Mapping_Tools", assetNamePattern);
                var hasUpdate = await updateManager.FetchUpdateAsync();

                if (!hasUpdate) {
                    if (notifyUser)
                        MessageQueue.Enqueue("No new versions available.");
                    return;
                }

                // Check if this version is newer than the version we skip
                var skipVersion = SettingsManager.Settings.SkipVersion;
                if (allowSkip && skipVersion != null && !(updateManager.UpdatesResult.LastVersion > skipVersion)) {
                    if (notifyUser)
                        MessageQueue.Enqueue($"Version {updateManager.UpdatesResult.LastVersion} skipped because of user config.");
                    return;
                }

                Dispatcher.Invoke(() => {
                    updaterWindow = new UpdaterWindow(updateManager.Progress) {
                        ShowActivated = true
                    };

                    updaterWindow.Closed += DisposeUpdateManager;

                    updaterWindow.ActionSelected += async (_, action) => {
                        switch (action) {
                            case UpdateAction.Restart:
                                updateAfterClose = false;
                                await updateManager.DownloadUpdateAsync();
                                updateManager.RestartAfterUpdate = true;
                                updateManager.StartUpdateProcess();

                                updaterWindow.Close();
                                Close();
                                break;

                            case UpdateAction.Wait:
                                updateAfterClose = true;
                                updateManager.RestartAfterUpdate = false;
                                downloadUpdateTask = updateManager.DownloadUpdateAsync();

                                // Preserve the update manager so it can be used later to download the update
                                updaterWindow.Closed -= DisposeUpdateManager;

                                updaterWindow.Close();
                                break;

                            case UpdateAction.Skip:
                                updateAfterClose = false;
                                // Update the skip version so we skip this version in the future
                                SettingsManager.Settings.SkipVersion = updateManager.UpdatesResult.LastVersion;
                                updaterWindow.Close();
                                break;

                            default:
                                updaterWindow.Close();
                                break;
                        }
                    };

                    void DisposeUpdateManager(object o, EventArgs eventArgs) {
                        updateManager.Dispose();
                        updateManager = null;
                    }

                    updaterWindow.Show();
                });
            } catch (Exception e) {
                MessageBox.Show("UPDATER_EXCEPTION: " + e.Message);
                if (notifyUser) {
                    MessageQueue.Enqueue("Error fetching update: " + e.Message);
                }
            }
        }

        private void AutoUpdateOnClose() {
            if (!updateAfterClose || updateManager == null) {
                return;
            }

            if (downloadUpdateTask is { IsCompletedSuccessfully: true }) {
                updateManager.StartUpdateProcess();
                return;
            }

            if (downloadUpdateTask is null or { IsFaulted: true }) {
                downloadUpdateTask = updateManager.DownloadUpdateAsync();
            }

            updaterWindow = new UpdaterWindow(updateManager.Progress, true) {
                ShowActivated = true
            };

            _ = Dispatcher.Invoke(async () => {
                await downloadUpdateTask;
                updateManager.StartUpdateProcess();
                updaterWindow.Close();
            });

            updaterWindow.ShowDialog();
        }

        private void Window_Closing(object sender, EventArgs e) {
            // Perform saving of settings at application exit
            if (autoSave) {
                ViewModel.Views.AutoSaveSettings();
                if (ViewModel.View is MappingTool mt) {
                    mt.Dispose();
                }
                SettingsManager.UpdateSettings();
                SettingsManager.WriteToJson();
            }
            AutoUpdateOnClose();
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

        private bool IsUserAdministrator() {
            bool isAdmin;
            try {
                WindowsIdentity user = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(user);
                isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch( UnauthorizedAccessException ) {
                isAdmin = false;
            }
            catch( Exception ) {
                isAdmin = false;
            }
            return isAdmin;
        }

        public object GetCurrentView() {
            return ViewModel.View;
        }

        public void SetCurrentMaps(string[] paths) {
            string strPaths = string.Join("|", paths);
            ViewModel.CurrentBeatmaps = strPaths;
            UpdateCurrentBeatmap(strPaths);
            SettingsManager.AddRecentMap(paths, DateTime.Now);
        }

        public void SetCurrentMapsString(string paths) {
            ViewModel.CurrentBeatmaps = paths;
            UpdateCurrentBeatmap(paths);
            SettingsManager.AddRecentMap(paths.Split('|'), DateTime.Now);
        }

        public string[] GetCurrentMaps() {
            var maps = ViewModel.CurrentBeatmaps.Split('|');
            
            if (maps.Any(o => !File.Exists(o))) {
                MessageQueue.Enqueue("It seems like one of the selected beatmaps does not exist. Please re-select the file with 'File > Open beatmap'.", true);
            }

            return maps;
        }

        public string GetCurrentMapsString() {
            return string.Join("|", GetCurrentMaps());
        }

        private void OpenBeatmap(object sender, RoutedEventArgs e) {
            try {
                string[] paths = IOHelper.BeatmapFileDialog(true);
                if( paths.Length != 0 ) {
                    SetCurrentMaps(paths);
                }
            }
            catch( Exception ex ) {
                ex.Show();
            }
        }

        private async void OpenGetCurrentBeatmap(object sender, RoutedEventArgs e) {
            try {
                string path = await Task.Run(() => IOHelper.GetCurrentBeatmap());
                if (path != "") {
                    SetCurrentMaps(new[] { path });
                }
            } catch (Exception ex) {
                ex.Show();
            }
        }

        private async void SaveBackup(object sender, RoutedEventArgs e) {
            try {
                var paths = GetCurrentMaps();
                var result = await Task.Run(() => BackupManager.SaveMapBackup(paths, true, "UB"));  // UB stands for User Backup
                if (result) {
                    await Task.Run(() => MessageQueue.Enqueue($"Beatmap{( paths.Length == 1 ? "" : "s" )} successfully copied!"));
                }
            } catch (Exception ex) {
                ex.Show();
            }
        }

        private async void LoadBackup(object sender, RoutedEventArgs e) {
            try {
                var paths = GetCurrentMaps();
                if (paths.Length > 1) {
                    throw new Exception($"Can't load backup into multiple beatmaps. You currently have {paths.Length} beatmaps selected.");
                }
                var backupPaths = IOHelper.BeatmapFileDialog(SettingsManager.GetBackupsPath(), false);
                if (backupPaths.Length == 1) {
                    try {
                        await Task.Run(() => BackupManager.LoadMapBackup(backupPaths[0], paths[0], false));
                    } catch (BeatmapIncompatibleException ex) {
                        var exResult = ex.Show();
                        if (exResult == MessageBoxResult.Cancel) {
                            return;
                        }

                        var result = MessageBox.Show("Do you want to load the backup anyways?", "Load backup",
                            MessageBoxButton.YesNo);
                        if (result == MessageBoxResult.Yes) {
                            await Task.Run(() => BackupManager.LoadMapBackup(backupPaths[0], paths[0], true));
                        } else {
                            return;
                        }
                    }
                    await Task.Run(() => MessageQueue.Enqueue("Backup successfully loaded!"));
                }
            } catch (Exception ex) {
                ex.Show();
            }
        }

        //Open backup folder in file explorer
        private void OpenBackups(object sender, RoutedEventArgs e) {
            try {
                System.Diagnostics.Process.Start("explorer.exe", SettingsManager.GetBackupsPath());
            }
            catch( Exception ex ) {
                ex.Show();
            }
        }

        private void OpenConfig(object sender, RoutedEventArgs e) {
            try {
                System.Diagnostics.Process.Start("explorer.exe", AppDataPath);
            }
            catch( Exception ex ) {
                ex.Show();
            }
        }

        private void OpenWebsite(object sender, RoutedEventArgs e) {
            System.Diagnostics.Process.Start("explorer.exe", "https://mappingtools.github.io");
        }

        private async void CoolSave(object sender, RoutedEventArgs e) {
            try {
                await Task.Run(() => EditorReaderStuff.BetterSave());
            } catch (Exception ex) {
                ex.Show();
            }
        }

        //Open project in browser
        private void OpenGitHub(object sender, RoutedEventArgs e) {
            System.Diagnostics.Process.Start("explorer.exe", "https://github.com/OliBomby/Mapping_Tools");
        }

        //Open project in browser
        private void OpenDonate(object sender, RoutedEventArgs e) {
            System.Diagnostics.Process.Start("explorer.exe", "https://ko-fi.com/olibomby ");
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

            MessageBox.Show(builder.ToString(), "Info");
        }

        //Change top right icons on changed window state and set state variable
        private void Window_StateChanged(object sender, EventArgs e) {
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
            if (FindName("toggle_button") is not Button bt)
                return;

            if (fullscreen) {
                if (actuallyChangeFullscreen) {
                    WindowState = WindowState.Maximized;
                }

                MasterGrid.Margin = new Thickness(7);
                WindowBorder.BorderThickness = new Thickness(0);
                bt.Content = new PackIcon { Kind = PackIconKind.WindowRestore };
            } else {
                if (actuallyChangeFullscreen) {
                    WindowState = WindowState.Normal;
                }

                MasterGrid.Margin = new Thickness(0);
                WindowBorder.BorderThickness = new Thickness(1);
                bt.Content = new PackIcon { Kind = PackIconKind.WindowMaximize };
            }
            EnsureOnScreen();
        }

        private void SetToRect(Rect rect) {
            Left = MathHelper.Clamp(rect.Left, SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - 300);
            Top = MathHelper.Clamp(rect.Top, SystemParameters.VirtualScreenTop, SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - 100);
            Width = Math.Max(rect.Width, 300);
            Height = Math.Max(rect.Height, 100);
        }

        private void EnsureOnScreen() {
            SetToRect(new Rect(Left, Top, Width, Height));
        }

        //Minimize window on click
        private void MinimizeWin(object sender, RoutedEventArgs e) {
            WindowState = WindowState.Minimized;
        }

        //Enable drag control of window and set icons when docked
        private void DragWin(object sender, MouseButtonEventArgs e) {
            if (e.ChangedButton != MouseButton.Left)
                return;

            if (WindowState == WindowState.Maximized) {
                var point = PointToScreen(e.MouseDevice.GetPosition(this));
                Top = point.Y - (((FrameworkElement)sender).ActualHeight / 2);
                SetFullscreen(false);
            }

            if (e.LeftButton == MouseButtonState.Pressed) {
                DragMove();
            }

            e.Handled = true;
        }

        private async void MenuItem_OnClick(object sender, RoutedEventArgs e) {
            await Update(false, true);
        }
    }
}