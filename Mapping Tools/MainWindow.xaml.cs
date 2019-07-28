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

namespace Mapping_Tools {
    public partial class MainWindow : Window {
        private bool isMaximized = false; //Check for window state
        private double widthWin, heightWin; //Set default sizes of window
        public ViewCollection Views;
        public bool SessionhasAdminRights;

        public static MainWindow AppWindow { get; set; }
        public static readonly HttpClient HttpClient = new HttpClient();
        private static readonly string appCommon = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        public static readonly string AppDataPath = Path.Combine(appCommon, "Mapping Tools");
        public static readonly string ExportPath = Path.Combine(AppDataPath, "Exports");

        public MainWindow() {
            Setup();
            InitializeComponent();
            SettingsManager.LoadConfig();
            AppWindow = this;
            widthWin = ActualWidth; // Set width to window
            heightWin = ActualHeight; // Set height to window
            Views = new ViewCollection(); // Make a ViewCollection object
            DataContext = new StandardVM(); // Generate Standard view model to show on startup

            if(SettingsManager.GetRecentMaps().Count > 0 ) {
                SetCurrentMap(SettingsManager.GetRecentMaps()[0][0]);
            } // Set currentmap to previously opened map
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

        public void SetCurrentMap(string path) {
            currentMap.Text = path;
            SettingsManager.AddRecentMap(path, DateTime.Now);
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
            bool result = IOHelper.SaveMapBackup(GetCurrentMap(), forced: true);
            if (result)
                System.Windows.MessageBox.Show("Beatmap successfully copied!");
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
            textBox.Anchor |= AnchorStyles.Right;
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
            Views.AutoSaveSettings();
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
