using AutoUpdaterDotNET;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.Tools;
using Mapping_Tools.Viewmodels;
using Mapping_Tools.Views;
using MaterialDesignThemes.Wpf;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Principal;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Mapping_Tools {

    public partial class MainWindow {
        public bool IsMaximized; //Check for window state
        public double WidthWin, HeightWin; //Set default sizes of window
        public ViewCollection Views;
        public ListenerManager ListenerManager;
        public bool SessionhasAdminRights;

        public static MainWindow AppWindow { get; set; }
        public static readonly HttpClient HttpClient = new HttpClient();
        private static readonly string AppCommon = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        public static readonly string AppDataPath = Path.Combine(AppCommon, "Mapping Tools");
        public static readonly string ExportPath = Path.Combine(AppDataPath, "Exports");

        public MainWindow() {
            InitializeComponent();
            try {
                Setup();
                SettingsManager.LoadConfig();
                ListenerManager = new ListenerManager();
                AppWindow = this;
                IsMaximized = SettingsManager.Settings.MainWindowMaximized;
                WidthWin = SettingsManager.Settings.MainWindowWidth ?? Width;
                HeightWin = SettingsManager.Settings.MainWindowHeight ?? Height;
                IsMaximized = !IsMaximized;
                ToggleWin(this, null);
                WidthWin = SettingsManager.Settings.MainWindowWidth ?? Width;
                HeightWin = SettingsManager.Settings.MainWindowHeight ?? Height;
                Views = new ViewCollection(); // Make a ViewCollection object
                SetCurrentView(new StandardVM()); // Generate Standard view model to show on startup

                SetCurrentMaps(SettingsManager.GetLatestCurrentMaps()); // Set currentmap to previously opened map
            } catch (Exception ex) {
                MessageBox.Show($"{ex.Message}{Environment.NewLine}{ex.StackTrace}", "Error");
            }
        }

        private void Setup() {
            SessionhasAdminRights = IsUserAdministrator();

            try {
                AutoUpdater.ParseUpdateInfoEvent += AutoUpdaterOnParseUpdateInfoEvent;
                AutoUpdater.Start("https://mappingtools.seira.moe/current/updater.json");
            }
            catch( Exception ex ) {
                Console.WriteLine(ex.Message);
            }

            try {
                Directory.CreateDirectory(AppDataPath);
                Directory.CreateDirectory(ExportPath);
            }
            catch( Exception ex ) {
                MessageBox.Show(ex.Message);
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
            }
            catch( Exception ex ) {
                Console.WriteLine(ex.Message);
            }
        }

        public object GetCurrentView() {
            return DataContext;
        }

        public void SetCurrentView(object view) {
            if (DataContext is MappingTool mt) {
                mt.Deactivate();
            }
            if (view is MappingTool nmt) {
                nmt.Activate();
            }

            DataContext = view;
            ViewChanged();
        }

        public void SetCurrentMaps(string[] paths) {
            currentMap.Text = string.Join("|", paths);
            SettingsManager.AddRecentMap(paths, DateTime.Now);
        }

        public void SetCurrentMapsString(string paths) {
            currentMap.Text = paths;
            SettingsManager.AddRecentMap(paths.Split('|'), DateTime.Now);
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

        private void OpenGetCurrentBeatmap(object sender, RoutedEventArgs e) {
            string path = IOHelper.GetCurrentBeatmap();
            if( path != "" ) { SetCurrentMaps(new[] { path }); }
        }

        private void SaveBackup(object sender, RoutedEventArgs e) {
            var paths = GetCurrentMaps();
            var result = IOHelper.SaveMapBackup(paths, true);
            if( result )
                MessageBox.Show($"Beatmap{( paths.Length == 1 ? "" : "s" )} successfully copied!");
        }

        //Method for loading the cleaner interface
        private void LoadCleaner(object sender, RoutedEventArgs e) {
            SetCurrentView(Views.GetMapCleaner());

            if (FindName("header") is TextBlock txt) txt.Text = "Mapping Tools - Map Cleaner";

            MinWidth = 630;
            MinHeight = 560;
        }

        //Method for loading the cleaner interface
        private void LoadMetadataManager(object sender, RoutedEventArgs e) {
            SetCurrentView(Views.GetMetadataManager());

            if (this.FindName("header") is TextBlock txt) txt.Text = "Mapping Tools - Metadata Manager";

            MinWidth = 630;
            MinHeight = 560;
        }

        //Method for loading the property transformer
        private void LoadPropertyTransformer(object sender, RoutedEventArgs e) {
            SetCurrentView(Views.GetPropertyTransformer());

            if (this.FindName("header") is TextBlock txt) txt.Text = "Mapping Tools - Property Transformer";

            MinWidth = 630;
            MinHeight = 560;
        }

        //Method for loading the merger interface
        private void LoadMerger(object sender, RoutedEventArgs e) {
            SetCurrentView(Views.GetSliderMerger());

            if (this.FindName("header") is TextBlock txt) txt.Text = "Mapping Tools - Slider Merger";

            this.MinWidth = 400;
            this.MinHeight = 380;
        }

        //Method for loading the completionator interface
        private void LoadCompletionator(object sender, RoutedEventArgs e) {
            SetCurrentView(Views.GetSliderCompletionator());

            if (this.FindName("header") is TextBlock txt) txt.Text = "Mapping Tools - Slider Completionator";

            this.MinWidth = 400;
            this.MinHeight = 380;
        }

        //Method for loading the snapping tools interface
        private void LoadSnappingTools(object sender, RoutedEventArgs e) {
            SetCurrentView(Views.GetSnappingTools());

            if (this.FindName("header") is TextBlock txt) txt.Text = "Mapping Tools - Snapping Tools";

            this.MinWidth = 400;
            this.MinHeight = 380;
        }

        //Method for loading the timing copier interface
        private void LoadTimingCopier(object sender, RoutedEventArgs e) {
            SetCurrentView(Views.GetTimingCopier());

            if (this.FindName("header") is TextBlock txt) txt.Text = "Mapping Tools - Timing Copier";

            this.MinWidth = 400;
            this.MinHeight = 380;
        }

        //Method for loading the timing helper interface
        private void LoadTimingHelper(object sender, RoutedEventArgs e) {
            SetCurrentView(Views.GetTimingHelper());

            if (this.FindName("header") is TextBlock txt) txt.Text = "Mapping Tools - Timing Helper";

            this.MinWidth = 400;
            this.MinHeight = 380;
        }

        //Method for loading the hitsound copier
        private void LoadHSCopier(object sender, RoutedEventArgs e) {
            SetCurrentView(Views.GetHitsoundCopier());

            if (this.FindName("header") is TextBlock txt) txt.Text = "Mapping Tools - Hitsound Copier";

            this.MinWidth = 100;
            this.MinHeight = 100;
        }

        //Method for loading the hitsound studio
        private void LoadHSStudio(object sender, RoutedEventArgs e) {
            SetCurrentView(Views.GetHitsoundStudio());

            if (this.FindName("header") is TextBlock txt) txt.Text = "Mapping Tools - Hitsound Studio";

            this.MinWidth = 100;
            this.MinHeight = 100;
        }

        //Method for loading the hitsound preview helper
        private void LoadHSPreviewHelper(object sender, RoutedEventArgs e) {
            SetCurrentView(Views.GetHitsoundPreviewHelper());

            if (this.FindName("header") is TextBlock txt) txt.Text = "Mapping Tools - Hitsound Preview Helper";

            this.MinWidth = 100;
            this.MinHeight = 100;
        }

        //Method for loading the standard interface
        private void LoadStartup(object sender, RoutedEventArgs e) {
            SetCurrentView(Views.GetStandard());

            if (this.FindName("header") is TextBlock txt) txt.Text = "Mapping Tools";

            this.MinWidth = 100;
            this.MinHeight = 100;
        }

        //Method for loading the preferences
        private void LoadPreferences(object sender, RoutedEventArgs e) {
            SetCurrentView(Views.GetPreferences());

            if (this.FindName("header") is TextBlock txt) txt.Text = "Mapping Tools - Preferences";

            this.MinWidth = 100;
            this.MinHeight = 100;
        }

        private void ViewChanged() {
            if (!(FindName("project") is MenuItem menuitem)) return;
            var isSavable = DataContext.GetType().GetInterfaces().Any(x =>
                              x.IsGenericType &&
                              x.GetGenericTypeDefinition() == typeof(ISavable<>));
             menuitem.Visibility = isSavable ? Visibility.Visible : Visibility.Collapsed;
        }

        private void LoadProject(object sender, RoutedEventArgs e) {
            if( !ProjectManager.IsSavable(DataContext) )
                return;
            dynamic data = DataContext;
            ProjectManager.LoadProject(data, true);
        }

        private void SaveProject(object sender, RoutedEventArgs e) {
            if( !ProjectManager.IsSavable(DataContext) )
                return;
            dynamic data = DataContext;
            ProjectManager.SaveProject(data, true);
        }

        //Open backup folder in file explorer
        private void OpenBackups(object sender, RoutedEventArgs e) {
            try {
                System.Diagnostics.Process.Start(SettingsManager.GetBackupsPath());
            }
            catch( Exception ex ) {
                System.Windows.Forms.MessageBox.Show(ex.Message);
            }
        }

        private void OpenWebsite(object sender, RoutedEventArgs e) {
            System.Diagnostics.Process.Start("https://mappingtools.seira.moe/");
        }

        private void CoolSave(object sender, RoutedEventArgs e) {
            EditorReaderStuff.CoolSave();
        }

        //Open project in browser
        private void OpenGitHub(object sender, RoutedEventArgs e) {
            System.Diagnostics.Process.Start("https://github.com/OliBomby/Mapping_Tools");
        }

        //Open info screen
        private void OpenInfo(object sender, RoutedEventArgs e) {
            var version = Assembly.GetEntryAssembly()?.GetName().Version;
            MessageBox.Show($"Mapping Tools {version}\n\nMade by:\nOliBomby\nPotoofu", "Info");
        }

        //Change top right icons on changed window state and set state variable
        private void Window_StateChanged(object sender, EventArgs e) {
            if (!(this.FindName("toggle_button") is Button bt)) return;

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
            if (!(this.FindName("toggle_button") is Button bt)) return;

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
            if (DataContext is MappingTool mt){ mt.Deactivate(); }
            Views.AutoSaveSettings();
            SettingsManager.UpdateSettings();
            SettingsManager.WriteToJson();
            this.Close();
        }

        //Enable drag control of window and set icons when docked
        private void DragWin(object sender, MouseButtonEventArgs e) {
            if (e.ChangedButton != MouseButton.Left) return;
            if (!(this.FindName("toggle_button") is Button bt)) return;

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
            if( e.LeftButton == MouseButtonState.Pressed )
                this.DragMove();
            //bt.Content = new PackIcon { Kind = PackIconKind.WindowRestore };
        }
    }
}