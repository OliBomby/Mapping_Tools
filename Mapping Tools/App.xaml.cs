using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace Mapping_Tools {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e) {
            // Process unhandled exception
            var exception = e.Exception;
            var lines = new List<string> { exception.Message, exception.StackTrace, exception.Source };

            while (exception.InnerException != null) {
                exception = exception.InnerException;
                lines.Add("\nInner exception:");
                lines.Add(exception.Message);
                lines.Add(exception.StackTrace);
                lines.Add(exception.Source);
            }

            var path = Path.Combine("crash-log.txt");
            File.WriteAllLines(path, lines);
            MessageBox.Show($"The program encountered an unhandled exception. Look in crash-log.txt for more info:\n{path}", "Error");

            // Prevent default unhandled exception processing
            e.Handled = true;
        }
    }
}
