using System.Linq;
using System.Windows;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.SystemTools.QuickRun;
using MaterialDesignThemes.Wpf;

namespace Mapping_Tools.Views.Preferences
{
    /// <summary>
    /// Interaktionslogik für UserControl2.xaml
    /// </summary>
    public partial class PreferencesView {

        public static readonly string ToolName = "Preferences";

        public static readonly string ToolDescription = $@"";

        public PreferencesView()
        {
            InitializeComponent();
            DataContext = SettingsManager.Settings;

            var views = MainWindow.AppWindow.Views;
            NoneQuickRunBox.ItemsSource = new[] { "<Current Tool>" }.Concat(
                ViewCollection.GetNames(ViewCollection.GetAllQuickRunTypesWithTargets(SmartQuickRunTargets.NoSelection)));
            SingleQuickRunBox.ItemsSource = new[] {"<Current Tool>"}.Concat(
                ViewCollection.GetNames(ViewCollection.GetAllQuickRunTypesWithTargets(SmartQuickRunTargets.SingleSelection)));
            MultipleQuickRunBox.ItemsSource = new[] {"<Current Tool>"}.Concat(
                ViewCollection.GetNames(ViewCollection.GetAllQuickRunTypesWithTargets(SmartQuickRunTargets.MultipleSelection)));
        }

        
        private void MakeDark(object sender, RoutedEventArgs e) {
            var theme = new PaletteHelper().GetTheme();
            theme.SetBaseTheme(Theme.Dark);
            new PaletteHelper().SetTheme(theme);
        }

        
        private void MakeLight(object sender, RoutedEventArgs e) {
            var theme = new PaletteHelper().GetTheme();
            theme.SetBaseTheme(Theme.Light);
            new PaletteHelper().SetTheme(theme);
        }

        private void Button_LoadGameImport_Click(object sender, RoutedEventArgs e) {
            string path = IOHelper.FolderDialog();
            if (!string.IsNullOrWhiteSpace(path)) {
                SettingsManager.Settings.OsuPath = path;
            }
        }

        private void Button_LoadSongsImport_Click(object sender, RoutedEventArgs e) {
            string path = IOHelper.FolderDialog();
            if (!string.IsNullOrWhiteSpace(path)) {
                SettingsManager.Settings.SongsPath = path;
            }
        }

        private void Button_LoadBackupImport_Click(object sender, RoutedEventArgs e) {
            string path = IOHelper.FolderDialog();
            if (!string.IsNullOrWhiteSpace(path)) {
                SettingsManager.Settings.BackupsPath = path;
            }
        }

        private void Button_LoadConfigImport_Click(object sender, RoutedEventArgs e)
        {
            string path = IOHelper.ConfigFileDialog(SettingsManager.GetOsuPath());
            if (!string.IsNullOrWhiteSpace(path)) {
                SettingsManager.Settings.OsuConfigPath = path;
            }
        }
    }
}
