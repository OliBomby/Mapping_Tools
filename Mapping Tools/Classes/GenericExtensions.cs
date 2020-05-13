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
            MessageBox.Show(exception.MessageStackTrace(), "Error");
            var ex = exception.InnerException;
            while (ex != null) {
                MessageBox.Show(ex.MessageStackTrace(), "Inner exception");
                ex = ex.InnerException;
            }
        }

        public static string MessageStackTrace(this Exception exception) {
            return exception.Message + "\n\n" + exception.StackTrace;
        }
    }
}
