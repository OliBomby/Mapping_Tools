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
using Mapping_Tools.Classes.SystemTools.HitsoundMaker;
using System.Windows.Forms;
using System.Drawing;

namespace Mapping_Tools {
    public partial class MainWindow :Window {
        private bool isMaximized = false; //Check for window state
        private double widthWin, heightWin; //Set default sizes of window
        public static MainWindow AppWindow { get; set; }
        public SettingsManager settingsManager;
        public ProjectManager projectmanager;
        public ViewCollection Views;
        public bool SessionhasAdminRights;
        public string AppDataPath;
        public string BackupPath;
        public string HSProjectPath;
        public string ExportPath;

        public MainWindow() {
            Setup();
            InitializeComponent();
            AppWindow = this;
            widthWin = ActualWidth; // Set width to window
            heightWin = ActualHeight; // Set height to window
            Views = new ViewCollection(); // Make a ViewCollection object
            DataContext = new StandardVM(); // Generate Standard view model to show on startup
            settingsManager = new SettingsManager();
            projectmanager = new ProjectManager();

            if( settingsManager.GetRecentMaps().Count > 0 ) {
                SetCurrentMap(settingsManager.GetRecentMaps()[0][0]);
            } // Set currentmap to previously opened map
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
            ExportPath = Path.Combine(AppDataPath, "Exports");
            HSProjectPath = Path.Combine(AppDataPath, "Hitsounding Projects");

            try {
                Directory.CreateDirectory(AppDataPath);
                Directory.CreateDirectory(BackupPath);
                Directory.CreateDirectory(ExportPath);
                Directory.CreateDirectory(HSProjectPath);
            }
            catch( Exception ex ) {
                System.Windows.MessageBox.Show(ex.Message);
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
            string path = IOHelper.BeatmapFileDialog();
            if( path != "" ) { SetCurrentMap(path); }
        }

        private void OpenCurrentBeatmap(object sender, RoutedEventArgs e) {
            string path = IOHelper.CurrentBeatmap();
            if( path != "" ) { SetCurrentMap(path); }
        }

        private void SaveBackup(object sender, RoutedEventArgs e) {
            DateTime now = DateTime.Now;
            string fileToCopy = currentMap.Text;
            string destinationDirectory = BackupPath;
            try {
                File.Copy(fileToCopy, Path.Combine(destinationDirectory, now.ToString("yyyy-MM-dd HH-mm-ss") + "___" + System.IO.Path.GetFileName(fileToCopy)));
            }
            catch( Exception ex ) {
                System.Windows.MessageBox.Show(ex.Message);
                return;
            }
            System.Windows.MessageBox.Show("Beatmap successfully copied!");
        }

        //Method for loading the cleaner interface 
        private void LoadCleaner(object sender, RoutedEventArgs e) {
            DataContext = Views.GetMapCleaner();

            TextBlock txt = this.FindName("header") as TextBlock;
            txt.Text = "Mapping Tools - Map Cleaner";

            System.Windows.Controls.MenuItem menuitem = this.FindName("project") as System.Windows.Controls.MenuItem;
            menuitem.Visibility = Visibility.Collapsed;

            MinWidth = 630;
            MinHeight = 560;
        }

        //Method for loading the property transformer
        private void LoadPropertyTransformer(object sender, RoutedEventArgs e) {
            DataContext = Views.GetPropertyTransformer();

            TextBlock txt = this.FindName("header") as TextBlock;
            txt.Text = "Mapping Tools - Property Transformer";

            System.Windows.Controls.MenuItem menuitem = this.FindName("project") as System.Windows.Controls.MenuItem;
            menuitem.Visibility = Visibility.Collapsed;

            MinWidth = 630;
            MinHeight = 560;
        }

        //Method for loading the merger interface
        private void LoadMerger(object sender, RoutedEventArgs e) {
            DataContext = Views.GetSliderMerger();

            TextBlock txt = this.FindName("header") as TextBlock;
            txt.Text = "Mapping Tools - Slider Merger";

            System.Windows.Controls.MenuItem menuitem = this.FindName("project") as System.Windows.Controls.MenuItem;
            menuitem.Visibility = Visibility.Collapsed;

            this.MinWidth = 400;
            this.MinHeight = 380;
        }

        //Method for loading the completionator interface
        private void LoadCompletionator(object sender, RoutedEventArgs e) {
            DataContext = Views.GetSliderCompletionator();

            TextBlock txt = this.FindName("header") as TextBlock;
            txt.Text = "Mapping Tools - Slider Completionator";

            System.Windows.Controls.MenuItem menuitem = this.FindName("project") as System.Windows.Controls.MenuItem;
            menuitem.Visibility = Visibility.Collapsed;

            this.MinWidth = 400;
            this.MinHeight = 380;
        }

        //Method for loading the snapping tools interface
        private void LoadSnappingTools(object sender, RoutedEventArgs e) {
            DataContext = Views.GetSnappingTools();

            TextBlock txt = this.FindName("header") as TextBlock;
            txt.Text = "Mapping Tools - Snapping Tools";

            System.Windows.Controls.MenuItem menuitem = this.FindName("project") as System.Windows.Controls.MenuItem;
            menuitem.Visibility = Visibility.Collapsed;

            this.MinWidth = 400;
            this.MinHeight = 380;
        }

        //Method for loading the timing helper interface
        private void LoadTimingHelper(object sender, RoutedEventArgs e) {
            DataContext = Views.GetTimingHelper();

            TextBlock txt = this.FindName("header") as TextBlock;
            txt.Text = "Mapping Tools - TimingHelper";

            System.Windows.Controls.MenuItem menuitem = this.FindName("project") as System.Windows.Controls.MenuItem;
            menuitem.Visibility = Visibility.Collapsed;

            this.MinWidth = 400;
            this.MinHeight = 380;
        }

        //Method for loading the hitsound copier
        private void LoadHSCopier(object sender, RoutedEventArgs e) {
            DataContext = Views.GetHitsoundCopier();

            TextBlock txt = this.FindName("header") as TextBlock;
            txt.Text = "Mapping Tools - Hitsound Copier";

            System.Windows.Controls.MenuItem menuitem = this.FindName("project") as System.Windows.Controls.MenuItem;
            menuitem.Visibility = Visibility.Collapsed;

            this.MinWidth = 100;
            this.MinHeight = 100;
        }

        //Method for loading the hitsound maker
        private void LoadHSMaker(object sender, RoutedEventArgs e) {
            DataContext = Views.GetHitsoundMaker();

            TextBlock txt = this.FindName("header") as TextBlock;
            txt.Text = "Mapping Tools - Hitsound Maker";

            System.Windows.Controls.MenuItem menuitem = this.FindName("project") as System.Windows.Controls.MenuItem;
            menuitem.Visibility = Visibility.Visible;

            this.MinWidth = 100;
            this.MinHeight = 100;
        }

        //Method for loading the standard interface
        private void LoadStartup(object sender, RoutedEventArgs e) {
            DataContext = Views.GetStandard();

            TextBlock txt = this.FindName("header") as TextBlock;
            txt.Text = "Mapping Tools";

            System.Windows.Controls.MenuItem menuitem = this.FindName("project") as System.Windows.Controls.MenuItem;
            menuitem.Visibility = Visibility.Collapsed;

            this.MinWidth = 100;
            this.MinHeight = 100;
        }

        //Method for loading the preferences
        private void LoadPreferences(object sender, RoutedEventArgs e) {
            DataContext = Views.GetPreferences();

            TextBlock txt = this.FindName("header") as TextBlock;
            txt.Text = "Mapping Tools - Preferences";

            System.Windows.Controls.MenuItem menuitem = this.FindName("project") as System.Windows.Controls.MenuItem;
            menuitem.Visibility = Visibility.Collapsed;

            this.MinWidth = 100;
            this.MinHeight = 100;
        }

        private void CreateHSProject(object sender, RoutedEventArgs e) {
            String value = "";
            if( InputBox("New Hitsounding Project", "Please specify project name:", ref value) == System.Windows.Forms.DialogResult.OK ) {
                projectmanager.CreateProject(value);
            }
        }

        public static DialogResult InputBox(string title, string promptText, ref string value) {
            Form form = new Form();
            System.Windows.Forms.Label label = new System.Windows.Forms.Label();
            System.Windows.Forms.TextBox textBox = new System.Windows.Forms.TextBox();
            System.Windows.Forms.Button buttonOk = new System.Windows.Forms.Button();
            System.Windows.Forms.Button buttonCancel = new System.Windows.Forms.Button();

            form.Text = title;
            label.Text = promptText;
            textBox.Text = value;

            buttonOk.Text = "OK";
            buttonCancel.Text = "Cancel";
            buttonOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;

            label.SetBounds(9, 20, 372, 13);
            textBox.SetBounds(12, 36, 372, 20);
            buttonOk.SetBounds(228, 72, 75, 23);
            buttonCancel.SetBounds(309, 72, 75, 23);

            label.AutoSize = true;
            textBox.Anchor = textBox.Anchor | AnchorStyles.Right;
            buttonOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            form.ClientSize = new System.Drawing.Size(396, 107);
            form.Controls.AddRange(new System.Windows.Forms.Control[] { label, textBox, buttonOk, buttonCancel });
            form.ClientSize = new System.Drawing.Size(Math.Max(300, label.Right + 10), form.ClientSize.Height);
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.AcceptButton = buttonOk;
            form.CancelButton = buttonCancel;

            DialogResult dialogResult = form.ShowDialog();
            value = textBox.Text;
            return dialogResult;
        }

        private void LoadHSProject(object sender, RoutedEventArgs e) {
            projectmanager.LoadProject();
        }

        private void SaveHSProject(object sender, RoutedEventArgs e) {
            projectmanager.SaveProject();
        }

        //Open backup folder in file explorer
        private void OpenBackups(object sender, RoutedEventArgs e) {
            try {
                Process.Start(BackupPath);
            }
            catch( Exception ex ) {
                System.Windows.Forms.MessageBox.Show(ex.Message);
                return;
            }
        }

        //Open project in browser
        private void OpenGitHub(object sender, RoutedEventArgs e) {
            Process.Start("https://github.com/Potoofu/Mapping_Tools");
        }

        //Open info screen
        private void OpenInfo(object sender, RoutedEventArgs e) {
            System.Windows.MessageBox.Show("Mapping Tools v. 1.0\nmade by\nOliBomby\nPotoofu");
        }

        //Change top right icons on changed window state and set state variable
        private void Window_StateChanged(object sender, EventArgs e) {
            System.Windows.Controls.Button bt = this.FindName("toggle_button") as System.Windows.Controls.Button;

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
            System.Windows.Controls.Button bt = this.FindName("toggle_button") as System.Windows.Controls.Button;
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
            Views.SaveSettings();
            settingsManager.WriteToJSON(false);
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
                this.DragMove();
                //bt.Content = new PackIcon { Kind = PackIconKind.WindowRestore };
            }
        }
    }
}
