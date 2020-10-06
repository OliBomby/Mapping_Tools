using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace Mapping_Tools.Classes {
    public static class GenericExtensions
    {
        public static int RemoveAll<T>(this ObservableCollection<T> coll, Func<T, bool> condition) {
            var itemsToRemove = coll.Where(condition).ToList();

            foreach (var itemToRemove in itemsToRemove) {
                coll.Remove(itemToRemove);
            }

            return itemsToRemove.Count;
        }

        public static void Show(this Exception exception) {
            var result = MessageBox.Show(exception.MessageStackTrace(), "Error", MessageBoxButton.OKCancel);
            var ex = result == MessageBoxResult.OK ? exception.InnerException : null;
            while (ex != null) {
                result = MessageBox.Show(ex.MessageStackTrace(), "Inner exception", MessageBoxButton.OKCancel);
                ex = result == MessageBoxResult.OK ? ex.InnerException : null;
            }
        }

        public static string MessageStackTrace(this Exception exception) {
            return exception.Message + "\n\n" + exception.StackTrace;
        }
    }
}
