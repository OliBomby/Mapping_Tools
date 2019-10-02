using Mapping_Tools.Classes.SystemTools;
using MaterialDesignThemes.Wpf;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Mapping_Tools.Views
{
    /// <summary>
    /// Interaktionslogik für UserControl2.xaml
    /// </summary>
    public partial class PreferencesView : UserControl
    {

        public static readonly string ToolName = "Preferences";

        public static readonly string ToolDescription = $@"";

        public PreferencesView()
        {
            InitializeComponent();
            DataContext = SettingsManager.Settings;
        }

        [Obsolete]
        private void MakeDark(object sender, RoutedEventArgs e) {
            new PaletteHelper().SetLightDark(true);
        }

        [Obsolete]
        private void MakeLight(object sender, RoutedEventArgs e) {
            if (MessageBox.Show("No", "No", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            if (MessageBox.Show("No.", "No", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            if (MessageBox.Show("No..", "No", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            if (MessageBox.Show(".", "No", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            if (MessageBox.Show(".", "No", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            if (MessageBox.Show(".", "No", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            if (MessageBox.Show("You really don't want this.", "No", MessageBoxButton.YesNo) !=
                MessageBoxResult.Yes) return;
            if (MessageBox.Show(".", "No", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            if (MessageBox.Show("Just look at this message box.", "No", MessageBoxButton.YesNo) !=
                MessageBoxResult.Yes) return;
            if (MessageBox.Show("It's terrible.", "No", MessageBoxButton.YesNo) !=
                MessageBoxResult.Yes) return;
            if (MessageBox.Show(".", "No", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            if (MessageBox.Show("Your eyes must be hurting already.", "No", MessageBoxButton.YesNo) !=
                MessageBoxResult.Yes) return;
            if (MessageBox.Show(".", "No", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            if (MessageBox.Show("Just.", "No", MessageBoxButton.YesNo) !=
                MessageBoxResult.Yes) return;
            if (MessageBox.Show(".", "No", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            if (MessageBox.Show("Why.", "No", MessageBoxButton.YesNo) !=
                MessageBoxResult.Yes) return;
            if (MessageBox.Show("You WILL regret this.", "No", MessageBoxButton.YesNo) !=
                MessageBoxResult.Yes) return;
            if (MessageBox.Show(".", "No", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            if (MessageBox.Show(".", "No", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            if (MessageBox.Show("This is a bad idea.", "No", MessageBoxButton.YesNo) !=
                MessageBoxResult.Yes) return;
            if (MessageBox.Show(".", "No", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            if (MessageBox.Show("You will literally punch your own eyes.", "No", MessageBoxButton.YesNo) !=
                MessageBoxResult.Yes) return;
            if (MessageBox.Show(".", "No", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            if (MessageBox.Show(".", "No", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            if (MessageBox.Show("You're probably thinking I have no idea what I'm talking about.", "No", MessageBoxButton.YesNo) !=
                MessageBoxResult.Yes) return;
            if (MessageBox.Show("I've been through this before.", "No", MessageBoxButton.YesNo) !=
                MessageBoxResult.Yes) return;
            if (MessageBox.Show(".", "No", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            if (MessageBox.Show("You hated it so much that you uninstalled me.", "No", MessageBoxButton.YesNo) !=
                MessageBoxResult.Yes) return;
            if (MessageBox.Show(".", "No", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            if (MessageBox.Show(".", "No", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            if (MessageBox.Show(".", "No", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            if (MessageBox.Show("Please.", "No", MessageBoxButton.YesNo) !=
                MessageBoxResult.Yes) return;
            if (MessageBox.Show("I have a family.", "No", MessageBoxButton.YesNo) !=
                MessageBoxResult.Yes) return;
            if (MessageBox.Show(".", "No", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            if (MessageBox.Show("Don't make me do this.", "No", MessageBoxButton.YesNo) !=
                MessageBoxResult.Yes) return;
            if (MessageBox.Show(".", "No", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            if (MessageBox.Show("OH god.", "No", MessageBoxButton.YesNo) !=
                MessageBoxResult.Yes) return;
            if (MessageBox.Show(".", "No", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            if (MessageBox.Show(".", "No", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            if (MessageBox.Show("Don't blame me after this.", "No", MessageBoxButton.YesNo) !=
                MessageBoxResult.Yes) return;
            if (MessageBox.Show(".", "No", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            if (MessageBox.Show("This is your last warning.", "No", MessageBoxButton.YesNo) !=
                MessageBoxResult.Yes) return;
            if (MessageBox.Show("...", "No", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            if (MessageBox.Show("Goodbye cruel world.", "No", MessageBoxButton.YesNo) !=
                MessageBoxResult.Yes) return;
            if (MessageBox.Show("I thought we could be friends...", "No", MessageBoxButton.YesNo) !=
                MessageBoxResult.Yes) return;

            new PaletteHelper().SetLightDark(false);
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            string path = IOHelper.FolderDialog();
            if (!string.IsNullOrWhiteSpace(path)) {
                SettingsManager.Settings.OsuPath = path;
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e) {
            string path = IOHelper.FolderDialog();
            if (!string.IsNullOrWhiteSpace(path)) {
                SettingsManager.Settings.SongsPath = path;
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e) {
            string path = IOHelper.FolderDialog();
            if (!string.IsNullOrWhiteSpace(path)) {
                SettingsManager.Settings.BackupsPath = path;
            }
        }

        private void Button_Click_3(object sender, RoutedEventArgs e) {
            MainWindow.AppWindow.ListenerManager.ReloadHotkeys();
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            string path = IOHelper.ConfigFileDialog(SettingsManager.GetOsuPath());
            if (!string.IsNullOrWhiteSpace(path)) {
                SettingsManager.Settings.OsuConfigPath = path;
            }
        }
    }
}
