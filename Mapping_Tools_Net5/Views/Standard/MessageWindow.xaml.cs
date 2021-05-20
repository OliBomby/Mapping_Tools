using Mapping_Tools.Classes.SystemTools;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Mapping_Tools.Views.Standard
{
    /// <summary>
    /// Interaction logic for MessageWindow.xaml
    /// </summary>
    public partial class MessageWindow : Window
    {
        public MessageWindow()
        {
            InitializeComponent();
        }

        public MessageWindow(ErrorType errorType, String Message = null, string title = null, RunWorkerCompletedEventArgs eventArg = null)
        {
            InitializeComponent();
            if(errorType == ErrorType.Success && title != null)
            {
                LoadSuccessWindow(Message,title);
            }
            else if (errorType == ErrorType.Success && title == null)
            {
                LoadSuccessWindow(Message);
            }
            else if (errorType == ErrorType.Error)
            {
                LoadErrorWindow(eventArg);
            }

        }

        private void LoadErrorWindow(RunWorkerCompletedEventArgs e)
        {
            MessageTitle.Content = "Error";
            MessageText.Text = e.Error.Message;
            ExceptionDetails.Text = e.Error.StackTrace;
        }

        private void LoadSuccessWindow(string message)
        {
            //Since we are only showing the Success of an event, we don't need the Expander
            ErrorExpander.Visibility = Visibility.Hidden;

            MessageTitle.Content = "Success";
            MessageText.Text = message;
        }

        private void LoadSuccessWindow(string message, string title)
        {
            //Since we are only showing the Success of an event, we don't need the Expander
            ErrorExpander.Visibility = Visibility.Hidden;

            MessageTitle.Content = title;
            MessageText.Text = message;
        }

        private void CloseWin(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        //Enable drag control of window and set icons when docked
        private void DragWin(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                Button bt = this.FindName("toggle_button") as Button;
                this.DragMove();
                //bt.Content = new PackIcon { Kind = PackIconKind.WindowRestore };
            }
        }
    }
}
