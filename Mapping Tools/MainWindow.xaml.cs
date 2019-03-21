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

namespace Mapping_Tools {
    public partial class MainWindow :Window {
        private bool isMaximized = false; //Check for window state
        private double widthWin, heightWin; //Set default sizes of window
        public static MainWindow AppWindow { get; set; }
        public SettingsManager settingsManager;
        public bool SessionhasAdminRights;
        public string AppDataPath;
        public string BackupPath;

        public MainWindow() {
            Setup();
            InitializeComponent();
            AppWindow = this;
            widthWin = ActualWidth; //Set width to window
            heightWin = ActualHeight; //Set height to window
            DataContext = new StandardVM(); //Generate Standard view model to show on startup
            settingsManager = new SettingsManager();
            if (settingsManager.GetRecentMaps().Count > 0) {
                SetCurrentMap(settingsManager.GetRecentMaps()[0][0]); } //Set currentmap to previously opened map
        }

        private void Setup() {
            SessionhasAdminRights = IsUserAdministrator() ? true : false;

            /*
            AutoUpdater.ParseUpdateInfoEvent += AutoUpdaterOnParseUpdateInfoEvent;
            AutoUpdater.Start("https://osu-mappingtools.potoofu.moe/Updater/updater.json");
            */

            string appCommon = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            AppDataPath = Path.Combine(appCommon, "Mapping-Tools");
            BackupPath = Path.Combine(AppDataPath, "Backups");
            
            if( !Directory.Exists(AppDataPath) ) {
                try {
                    Directory.CreateDirectory(AppDataPath);
                    Directory.CreateDirectory(BackupPath);
                }
                catch( Exception ex ) {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private bool IsUserAdministrator() {
            bool isAdmin;
            try {
                WindowsIdentity user = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(user);
                isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch( UnauthorizedAccessException) {
                isAdmin = false;
            }
            catch( Exception ) {
                isAdmin = false;
            }
            return isAdmin;
        }

        private void AutoUpdaterOnParseUpdateInfoEvent(ParseUpdateInfoEventArgs args) {
            dynamic json = JsonConvert.DeserializeObject(args.RemoteData);
            args.UpdateInfo = new UpdateInfoEventArgs {
                CurrentVersion = json.version,
                ChangelogURL = json.changelog,
                Mandatory = json.mandatory,
                DownloadURL = json.url
            };
        }
          
        public void SetCurrentMap(string path) {
            currentMap.Text = path;
            settingsManager.AddRecentMaps(path, DateTime.Now, true);
        }

        public string GetCurrentMap() {
            return currentMap.Text;
        }

        private void OpenBeatmap(object sender, RoutedEventArgs e) {
            string path = BeatmapFinder.FileDialog();
            if (path != "") { SetCurrentMap(path); }
        }

        private void OpenCurrentBeatmap(object sender, RoutedEventArgs e) {
            string path = BeatmapFinder.CurrentBeatmap();
            if (path != "") { SetCurrentMap(path); }
        }

        private void SaveBackup(object sender, RoutedEventArgs e) {
            DateTime now = DateTime.Now;
            string fileToCopy = currentMap.Text;
            string destinationDirectory = BackupPath;
            try {
                File.Copy(fileToCopy, Path.Combine(destinationDirectory, now.ToString("yyyy-MM-dd HH-mm-ss") + "___" + System.IO.Path.GetFileName(fileToCopy)));
            }
            catch( Exception ex ) {
                MessageBox.Show(ex.Message);
                return;
            }
            MessageBox.Show("Beatmap successfully copied!");
        }

        //Method for loading the cleaner interface 
        private void LoadCleaner(object sender, RoutedEventArgs e) {
            DataContext = new CleanerVM();

            TextBlock txt = this.FindName("header") as TextBlock;
            txt.Text = "Mapping Tools - Map Cleaner";

            MinWidth = 630;
            MinHeight = 560;
        }

        //Method for loading the merger interface
        private void LoadMerger(object sender, RoutedEventArgs e) {
            DataContext = new SliderMergerVM();

            TextBlock txt = this.FindName("header") as TextBlock;
            txt.Text = "Mapping Tools - Slider Merger";

            this.MinWidth = 400;
            this.MinHeight = 380;
        }

        //Method for loading the completionator interface
        private void LoadCompletionator(object sender, RoutedEventArgs e) {
            DataContext = new SliderCompletionatorVM();

            TextBlock txt = this.FindName("header") as TextBlock;
            txt.Text = "Mapping Tools - Slider Completionator";

            this.MinWidth = 400;
            this.MinHeight = 380;
        }

        //Method for loading the snapping tools interface
        private void LoadSnappingTools(object sender, RoutedEventArgs e) {
            DataContext = new SnappingToolsVM();

            TextBlock txt = this.FindName("header") as TextBlock;
            txt.Text = "Mapping Tools - Snapping Tools";

            this.MinWidth = 400;
            this.MinHeight = 380;
        }

        //Method for loading the hitsound copier
        private void LoadCopier(object sender, RoutedEventArgs e) {
            DataContext = new HitsoundCopierVM();
            TextBlock txt = this.FindName("header") as TextBlock;
            txt.Text = "Mapping Tools - Hitsound Copier";
            this.MinWidth = 100;
            this.MinHeight = 100;
        }

        //Method for loading the standard interface
        private void LoadStartup(object sender, RoutedEventArgs e) {
            DataContext = new StandardVM();
            TextBlock txt = this.FindName("header") as TextBlock;
            txt.Text = "Mapping Tools";
            this.MinWidth = 100;
            this.MinHeight = 100;
        }

        //Method for loading the preferences
        private void LoadPreferences(object sender, RoutedEventArgs e) {
            DataContext = new PreferencesVM();
            TextBlock txt = this.FindName("header") as TextBlock;
            txt.Text = "Mapping Tools - Preferences";
            this.MinWidth = 100;
            this.MinHeight = 100;
        }

        //Open backup folder in file explorer
        private void OpenBackups(object sender, RoutedEventArgs e) {
            try {
                Process.Start(BackupPath);
            }
            catch( Exception ex ) {
                MessageBox.Show(ex.Message);
                return;
            }
        }

        //Open project in browser
        private void OpenGitHub(object sender, RoutedEventArgs e) {
            Process.Start("https://github.com/Potoofu/Mapping_Tools");
        }

        //Open info screen
        private void OpenInfo(object sender, RoutedEventArgs e) {
            MessageBox.Show("Mapping Tools v. 1.0\nmade by\nOliBomby\nPotoofu");
        }

        //Change top right icons on changed window state and set state variable
        private void Window_StateChanged(object sender, EventArgs e) {
            Button bt = this.FindName("toggle_button") as Button;

            switch( this.WindowState ) {
                case WindowState.Maximized:
                    bt.Content = new PackIcon { Kind = PackIconKind.WindowRestore };
                    isMaximized = true;
                    window_border.BorderThickness = new Thickness(0);
                    break;
                case WindowState.Minimized:
                    break;
                case WindowState.Normal:
                    window_border.BorderThickness = new Thickness(1);
                    isMaximized = false;
                    bt.Content = new PackIcon { Kind = PackIconKind.WindowMaximize };
                    break;
            }
        }

        //Clickevent for top right maximize/minimize button
        private void ToggleWin(object sender, RoutedEventArgs e) {
            Button bt = this.FindName("toggle_button") as Button;
            if( isMaximized ) {
                this.WindowState = WindowState.Normal;
                Width = widthWin;
                Height = heightWin;
                isMaximized = false;
                window_border.BorderThickness = new Thickness(1);
                bt.Content = new PackIcon { Kind = PackIconKind.WindowMaximize };
            }
            else {
                widthWin = ActualWidth;
                heightWin = ActualHeight;
                this.Left = SystemParameters.WorkArea.Left;
                this.Top = SystemParameters.WorkArea.Top;
                this.Height = SystemParameters.WorkArea.Height;
                this.Width = SystemParameters.WorkArea.Width;
                window_border.BorderThickness = new Thickness(0);
                //this.WindowState = WindowState.Maximized;
                isMaximized = true;
                bt.Content = new PackIcon { Kind = PackIconKind.WindowRestore };
            }
        }

        //Minimize window on click
        private void MinimizeWin(object sender, RoutedEventArgs e) {
            this.WindowState = WindowState.Minimized;
        }

        //Close window
        private void CloseWin(object sender, RoutedEventArgs e) {
            settingsManager.WriteToJSON(false);
            this.Close();
        }

        //Enable drag control of window and set icons when docked
        private void DragWin(object sender, MouseButtonEventArgs e) {
            if( e.ChangedButton == MouseButton.Left ) {
                Button bt = this.FindName("toggle_button") as Button;
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
                this.DragMove();
                //bt.Content = new PackIcon { Kind = PackIconKind.WindowRestore };
            }
        }
    }
}
