using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf;
using Mapping_Tools.Viewmodels;
using Microsoft.Win32;
using System.IO;
using Mapping_Tools.Classes.SystemTools;
using AutoUpdaterDotNET;
using Newtonsoft.Json;
using System.Security.Principal;
using Mapping_Tools.Views;
using System.Windows.Forms;
using System.Drawing;
using System.Reflection;
using System.Linq;
using System.Net.Http;
using NonInvasiveKeyboardHookLibrary;
using System.Runtime.InteropServices;
using System.Threading;

namespace Mapping_Tools {
    public partial class MainWindow : Window {
        public bool IsMaximized; //Check for window state
        public double WidthWin, HeightWin; //Set default sizes of window
        public ViewCollection Views;
        public bool SessionhasAdminRights;

        public static MainWindow AppWindow { get; set; }
        public static readonly HttpClient HttpClient = new HttpClient();
        public static readonly KeyboardHookManager keyboardHookManager = new KeyboardHookManager();
        private static readonly string appCommon = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        public static readonly string AppDataPath = Path.Combine(appCommon, "Mapping Tools");
        public static readonly string ExportPath = Path.Combine(AppDataPath, "Exports");

        public MainWindow() {
            Setup();
            InitializeComponent();
            SettingsManager.LoadConfig();
            AppWindow = this;
            IsMaximized = SettingsManager.Settings.MainWindowMaximized;
            WidthWin = SettingsManager.Settings.MainWindowWidth ?? Width;
            HeightWin = SettingsManager.Settings.MainWindowHeight ?? Height;
            IsMaximized = !IsMaximized;
            ToggleWin(this, null);
            WidthWin = SettingsManager.Settings.MainWindowWidth ?? Width;
            HeightWin = SettingsManager.Settings.MainWindowHeight ?? Height;
            Views = new ViewCollection(); // Make a ViewCollection object
            DataContext = new StandardVM(); // Generate Standard view model to show on startup

            SetCurrentMaps(SettingsManager.GetLatestCurrentMaps()); // Set currentmap to previously opened map
            ViewChanged();
        }

        private void Setup() {
            SessionhasAdminRights = IsUserAdministrator() ? true : false;

            try {
                AutoUpdater.ParseUpdateInfoEvent += AutoUpdaterOnParseUpdateInfoEvent;
                AutoUpdater.Start("https://mappingtools.seira.moe/current/updater.json");
            } catch(Exception ex) {
                Console.WriteLine(ex.Message);
            }
            

            try {
                Directory.CreateDirectory(AppDataPath);
                Directory.CreateDirectory(ExportPath);
            }
            catch( Exception ex ) {
                System.Windows.MessageBox.Show(ex.Message);
            }

            // Register virtual key code 0x4D = M to QuickRun
            keyboardHookManager.RegisterHotkey(new[] { NonInvasiveKeyboardHookLibrary.ModifierKeys.Alt, NonInvasiveKeyboardHookLibrary.ModifierKeys.Control, NonInvasiveKeyboardHookLibrary.ModifierKeys.Shift }, 0x4D, QuickRunCurrentTool);
            keyboardHookManager.Start();
        }

        private void QuickRunCurrentTool() {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                if (DataContext is IQuickRun tool) {
                    tool.RunFinished -= Reload;
                    tool.RunFinished += Reload;
                    tool.QuickRun();
                }
            });
        }

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private void Reload(object sender, EventArgs e) {
            if (((RunToolCompletedEventArgs)e).NeedReload) {
                var proc = Process.GetProcessesByName("osu!").FirstOrDefault();;
                if (proc != null) {
                    var oldHandle = GetForegroundWindow();
                    if (oldHandle != proc.MainWindowHandle) {
                        SetForegroundWindow(proc.MainWindowHandle);
                        Thread.Sleep(300);
                    }
                }
                SendKeys.SendWait("^{L 10}");
                Thread.Sleep(100);
                SendKeys.SendWait("{ENTER}");
            }
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

        private void AutoUpdaterOnParseUpdateInfoEvent(ParseUpdateInfoEventArgs args) {
            try {
                dynamic json = JsonConvert.DeserializeObject(args.RemoteData);
                args.UpdateInfo = new UpdateInfoEventArgs {
                    CurrentVersion = json.version,
                    ChangelogURL = json.changelog,
                    Mandatory = json.mandatory,
                    DownloadURL = json.url
                };
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }

        public void SetCurrentMaps(string[] paths) {
            currentMap.Text = string.Join("|", paths);
            SettingsManager.AddRecentMap(currentMap.Text, DateTime.Now);
        }

        public void SetCurrentMapsString(string paths) {
            currentMap.Text = paths;
            SettingsManager.AddRecentMap(paths, DateTime.Now);
        }

        public string[] GetCurrentMaps() {
            return currentMap.Text.Split('|');
        }

        public string GetCurrentMapsString() {
            return string.Join("|", GetCurrentMaps());
        }

        private void OpenBeatmap(object sender, RoutedEventArgs e) {
            string[] paths = IOHelper.BeatmapFileDialog(true);
            if( paths.Length != 0 ) { SetCurrentMaps(paths); }
        }

        private void OpenCurrentBeatmap(object sender, RoutedEventArgs e) {
            string path = IOHelper.CurrentBeatmap();
            if( path != "" ) { SetCurrentMaps(new[] { path }); }
        }

        private void SaveBackup(object sender, RoutedEventArgs e) {
            var paths = GetCurrentMaps();
            bool result = IOHelper.SaveMapBackup(paths, true);
            if (result)
                System.Windows.MessageBox.Show($"Beatmap{(paths.Length == 1 ? "" : "s")} successfully copied!");
        }

        //Method for loading the cleaner interface 
        private void LoadCleaner(object sender, RoutedEventArgs e) {
            DataContext = Views.GetMapCleaner();

            TextBlock txt = this.FindName("header") as TextBlock;
            txt.Text = "Mapping Tools - Map Cleaner";

            ViewChanged();

            MinWidth = 630;
            MinHeight = 560;
        }

        //Method for loading the cleaner interface 
        private void LoadMetadataManager(object sender, RoutedEventArgs e) {
            DataContext = Views.GetMetadataManager();

            TextBlock txt = this.FindName("header") as TextBlock;
            txt.Text = "Mapping Tools - Metadata Manager";

            ViewChanged();

            MinWidth = 630;
            MinHeight = 560;
        }

        //Method for loading the property transformer
        private void LoadPropertyTransformer(object sender, RoutedEventArgs e) {
            DataContext = Views.GetPropertyTransformer();

            TextBlock txt = this.FindName("header") as TextBlock;
            txt.Text = "Mapping Tools - Property Transformer";

            ViewChanged();

            MinWidth = 630;
            MinHeight = 560;
        }

        //Method for loading the merger interface
        private void LoadMerger(object sender, RoutedEventArgs e) {
            DataContext = Views.GetSliderMerger();

            TextBlock txt = this.FindName("header") as TextBlock;
            txt.Text = "Mapping Tools - Slider Merger";

            ViewChanged();

            this.MinWidth = 400;
            this.MinHeight = 380;
        }

        //Method for loading the completionator interface
        private void LoadCompletionator(object sender, RoutedEventArgs e) {
            DataContext = Views.GetSliderCompletionator();

            TextBlock txt = this.FindName("header") as TextBlock;
            txt.Text = "Mapping Tools - Slider Completionator";

            ViewChanged();

            this.MinWidth = 400;
            this.MinHeight = 380;
        }

        //Method for loading the snapping tools interface
        private void LoadSnappingTools(object sender, RoutedEventArgs e) {
            DataContext = Views.GetSnappingTools();

            TextBlock txt = this.FindName("header") as TextBlock;
            txt.Text = "Mapping Tools - Snapping Tools";

            ViewChanged();

            this.MinWidth = 400;
            this.MinHeight = 380;
        }

        //Method for loading the timing copier interface
        private void LoadTimingCopier(object sender, RoutedEventArgs e) {
            DataContext = Views.GetTimingCopier();

            TextBlock txt = this.FindName("header") as TextBlock;
            txt.Text = "Mapping Tools - Timing Copier";

            ViewChanged();

            this.MinWidth = 400;
            this.MinHeight = 380;
        }

        //Method for loading the timing helper interface
        private void LoadTimingHelper(object sender, RoutedEventArgs e) {
            DataContext = Views.GetTimingHelper();

            TextBlock txt = this.FindName("header") as TextBlock;
            txt.Text = "Mapping Tools - Timing Helper";

            ViewChanged();

            this.MinWidth = 400;
            this.MinHeight = 380;
        }

        //Method for loading the hitsound copier
        private void LoadHSCopier(object sender, RoutedEventArgs e) {
            DataContext = Views.GetHitsoundCopier();

            TextBlock txt = this.FindName("header") as TextBlock;
            txt.Text = "Mapping Tools - Hitsound Copier";

            ViewChanged();

            this.MinWidth = 100;
            this.MinHeight = 100;
        }

        //Method for loading the hitsound studio
        private void LoadHSStudio(object sender, RoutedEventArgs e) {
            DataContext = Views.GetHitsoundStudio();

            TextBlock txt = this.FindName("header") as TextBlock;
            txt.Text = "Mapping Tools - Hitsound Studio";

            ViewChanged();

            this.MinWidth = 100;
            this.MinHeight = 100;
        }

        //Method for loading the hitsound preview helper
        private void LoadHSPreviewHelper(object sender, RoutedEventArgs e) {
            DataContext = Views.GetHitsoundPreviewHelper();

            TextBlock txt = this.FindName("header") as TextBlock;
            txt.Text = "Mapping Tools - Hitsound Preview Helper";

            ViewChanged();

            this.MinWidth = 100;
            this.MinHeight = 100;
        }

        //Method for loading the standard interface
        private void LoadStartup(object sender, RoutedEventArgs e) {
            DataContext = Views.GetStandard();

            TextBlock txt = this.FindName("header") as TextBlock;
            txt.Text = "Mapping Tools";

            ViewChanged();

            this.MinWidth = 100;
            this.MinHeight = 100;
        }

        //Method for loading the preferences
        private void LoadPreferences(object sender, RoutedEventArgs e) {
            DataContext = Views.GetPreferences();

            TextBlock txt = this.FindName("header") as TextBlock;
            txt.Text = "Mapping Tools - Preferences";

            ViewChanged();

            this.MinWidth = 100;
            this.MinHeight = 100;
        }

        private void ViewChanged() {
            System.Windows.Controls.MenuItem menuitem = this.FindName("project") as System.Windows.Controls.MenuItem;
            bool isSavable = DataContext.GetType().GetInterfaces().Any(x =>
                              x.IsGenericType &&
                              x.GetGenericTypeDefinition() == typeof(ISavable<>));
            menuitem.Visibility = isSavable ? Visibility.Visible : Visibility.Collapsed;
        }

        private void LoadProject(object sender, RoutedEventArgs e) {
            if (!ProjectManager.IsSavable(DataContext))
                return;
            dynamic data = DataContext;
            ProjectManager.LoadProject(data, true);
        }

        private void SaveProject(object sender, RoutedEventArgs e) {
            if (!ProjectManager.IsSavable(DataContext))
                return;
            dynamic data = DataContext;
            ProjectManager.SaveProject(data, true);
        }

        //Open backup folder in file explorer
        private void OpenBackups(object sender, RoutedEventArgs e) {
            try {
                Process.Start(SettingsManager.GetBackupsPath());
            }
            catch( Exception ex ) {
                System.Windows.Forms.MessageBox.Show(ex.Message);
                return;
            }
        }

        private void OpenWebsite(object sender, RoutedEventArgs e) {
            Process.Start("https://mappingtools.seira.moe/");
        }

        //Open project in browser
        private void OpenGitHub(object sender, RoutedEventArgs e) {
            Process.Start("https://github.com/OliBomby/Mapping_Tools");
        }

        //Open info screen
        private void OpenInfo(object sender, RoutedEventArgs e) {
            Version version = Assembly.GetEntryAssembly().GetName().Version;
            System.Windows.MessageBox.Show(string.Format("Mapping Tools {0}\n\nMade by:\nOliBomby\nPotoofu", version.ToString()), "Info");
        }

        //Change top right icons on changed window state and set state variable
        private void Window_StateChanged(object sender, EventArgs e) {
            System.Windows.Controls.Button bt = this.FindName("toggle_button") as System.Windows.Controls.Button;

            switch( this.WindowState ) {
                case WindowState.Maximized:
                    bt.Content = new PackIcon { Kind = PackIconKind.WindowRestore };
                    IsMaximized = true;
                    window_border.BorderThickness = new Thickness(0);
                    break;
                case WindowState.Minimized:
                    break;
                case WindowState.Normal:
                    window_border.BorderThickness = new Thickness(1);
                    IsMaximized = false;
                    bt.Content = new PackIcon { Kind = PackIconKind.WindowMaximize };
                    break;
            }
        }

        //Clickevent for top right maximize/minimize button
        private void ToggleWin(object sender, RoutedEventArgs e) {
            System.Windows.Controls.Button bt = this.FindName("toggle_button") as System.Windows.Controls.Button;
            if( IsMaximized ) {
                this.WindowState = WindowState.Normal;
                Width = WidthWin;
                Height = HeightWin;
                IsMaximized = false;
                window_border.BorderThickness = new Thickness(1);
                bt.Content = new PackIcon { Kind = PackIconKind.WindowMaximize };
            }
            else {
                WidthWin = ActualWidth;
                HeightWin = ActualHeight;
                this.Left = SystemParameters.WorkArea.Left;
                this.Top = SystemParameters.WorkArea.Top;
                this.Height = SystemParameters.WorkArea.Height;
                this.Width = SystemParameters.WorkArea.Width;
                window_border.BorderThickness = new Thickness(0);
                //this.WindowState = WindowState.Maximized;
                IsMaximized = true;
                bt.Content = new PackIcon { Kind = PackIconKind.WindowRestore };
            }
        }

        //Minimize window on click
        private void MinimizeWin(object sender, RoutedEventArgs e) {
            this.WindowState = WindowState.Minimized;
        }

        //Close window
        private void CloseWin(object sender, RoutedEventArgs e) {
            Views.AutoSaveSettings();
            SettingsManager.UpdateSettings();
            SettingsManager.WriteToJSON(false);
            this.Close();
        }

        //Enable drag control of window and set icons when docked
        private void DragWin(object sender, MouseButtonEventArgs e) {
            if( e.ChangedButton == MouseButton.Left ) {
                System.Windows.Controls.Button bt = this.FindName("toggle_button") as System.Windows.Controls.Button;
                if( WindowState == WindowState.Maximized ) {
                    var point = PointToScreen(e.MouseDevice.GetPosition(this));

                    if( point.X <= RestoreBounds.Width / 2 )
                        Left = 0;

                    else if( point.X >= RestoreBounds.Width )
                        Left = point.X - ( RestoreBounds.Width - ( this.ActualWidth - point.X ) );

                    else
                        Left = point.X - ( RestoreBounds.Width / 2 );

                    Top = point.Y - ( ( (FrameworkElement) sender ).ActualHeight / 2 );
                    WindowState = WindowState.Normal;
                    bt.Content = new PackIcon { Kind = PackIconKind.WindowMaximize };
                }
                if (e.LeftButton == MouseButtonState.Pressed)
                    this.DragMove();
                //bt.Content = new PackIcon { Kind = PackIconKind.WindowRestore };
            }
        }
    }
}
