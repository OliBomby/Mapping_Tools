using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf;
using Mapping_Tools.viewmodels;

namespace Mapping_Tools {
    public partial class MainWindow : Window {
        private bool isMaximized = false; //Check for window state
        private double widthWin, heightWin; //Set default sizes of window

        public MainWindow() {
            InitializeComponent();
            widthWin = ActualWidth; //Set width to window
            heightWin = ActualHeight; //Set height to window
            DataContext = new StandardVM(); //Generate Standard view model to show on startup
            
            //Check and create backup folder
            try {
                System.IO.Directory.CreateDirectory(System.Environment.CurrentDirectory + "\\Backups\\");
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message);
            }
        }
        
        //Method for loading the cleaner interface 
        private void LoadCleaner(object sender, RoutedEventArgs e) {
            DataContext = new CleanerVM();
            TextBlock txt = this.FindName("currentTool") as TextBlock;
            txt.Text = "Map_Cleaner";
            this.MinWidth = 800;
            this.MinHeight = 520;
        }

        //Method for loading the standard interface
        private void LoadCopier(object sender, RoutedEventArgs e) {
            DataContext = new StandardVM();
            TextBlock txt = this.FindName("currentTool") as TextBlock;
            txt.Text = "None";
            this.MinWidth = 100;
            this.MinHeight = 100;
        }

        //Open backup folder in file explorer
        private void OpenBackups(object sender, RoutedEventArgs e) {
            try {
                Process.Start(System.Environment.CurrentDirectory + "\\Backups\\");
            }
            catch (Exception ex) {
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
            switch (this.WindowState) {
                case WindowState.Maximized:
                    bt.Content = new PackIcon { Kind = PackIconKind.WindowRestore };
                    isMaximized = true;
                    break;
                case WindowState.Minimized:
                    break;
                case WindowState.Normal:
                    isMaximized = false;
                    bt.Content = new PackIcon { Kind = PackIconKind.WindowMaximize };
                    break;
            }
        }
    
        //Clickevent for top right maximize/minimize button
        private void ToggleWin(object sender, RoutedEventArgs e) {
            Button bt = this.FindName("toggle_button") as Button;
            if (isMaximized) {
                this.WindowState = WindowState.Normal;
                Width = widthWin;
                Height = heightWin;
                isMaximized = false;
                bt.Content = new PackIcon { Kind = PackIconKind.WindowMaximize };
            }
            else {
                widthWin = ActualWidth;
                heightWin = ActualHeight;
                Console.WriteLine(widthWin + "  " + heightWin);
                this.Left = SystemParameters.WorkArea.Left;
                this.Top = SystemParameters.WorkArea.Top;
                this.Height = SystemParameters.WorkArea.Height;
                this.Width = SystemParameters.WorkArea.Width;
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
            this.Close();
        }
    
        //Enable drag control of window and set icons when docked
        private void DragWin(object sender, MouseButtonEventArgs e) {
            if (e.ChangedButton == MouseButton.Left) {
                Button bt = this.FindName("toggle_button") as Button;
                if (WindowState == WindowState.Maximized) {
                    var point = PointToScreen(e.MouseDevice.GetPosition(this));

                    if (point.X <= RestoreBounds.Width / 2)
                        Left = 0;

                    else if (point.X >= RestoreBounds.Width)
                        Left = point.X - (RestoreBounds.Width - (this.ActualWidth - point.X));

                    else
                        Left = point.X - (RestoreBounds.Width / 2);

                    Top = point.Y - (((FrameworkElement)sender).ActualHeight / 2);
                    WindowState = WindowState.Normal;
                    bt.Content = new PackIcon { Kind = PackIconKind.WindowMaximize };
                }
                this.DragMove();
                bt.Content = new PackIcon { Kind = PackIconKind.WindowRestore };
            }
        }
    }
}
