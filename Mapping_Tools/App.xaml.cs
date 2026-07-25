using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using Mapping_Tools.Classes.SystemTools;

namespace Mapping_Tools {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e) {
         // Log the exception, display it, etc
            var exception = e.Exception;
            var lines = new List<string> { exception.Message, exception.StackTrace, exception.Source };

            while (exception.InnerException != null) {
                exception = exception.InnerException;
                lines.Add("\nInner exception:");
                lines.Add(exception.Message);
                lines.Add(exception.StackTrace);
                lines.Add(exception.Source);
            }

            const string filename = "crash-log.txt";
            var path = Path.Combine(SettingsManager.ApplicationDataPath, filename);
            File.WriteAllLines(path, lines);
            MessageBox.Show($"The program encountered an unhandled exception. Look in {filename} for more info:\n{path}", "Error");

            // Prevent default unhandled exception processing
            e.Handled = true;
        }
    }
}
