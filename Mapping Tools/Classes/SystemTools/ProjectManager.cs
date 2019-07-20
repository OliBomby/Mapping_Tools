using Mapping_Tools.Viewmodels;
using Mapping_Tools.Views;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Mapping_Tools.Classes.SystemTools {
    public static class ProjectManager {
        private static readonly JsonSerializer Serializer = new JsonSerializer {
            NullValueHandling = NullValueHandling.Ignore
        };

        public static void SaveProject<T>(ISavable<T> view, bool dialog=false) {
            if (dialog)
                Directory.CreateDirectory(view.DefaultSaveFolder);
            string path = dialog ? IOHelper.SaveProjectDialog(view.DefaultSaveFolder) : view.AutoSavePath;

            // If the file name is not an empty string open it for saving.  
            if (path != "") {
                try {
                    using (StreamWriter fs = new StreamWriter(path)) {
                        using (JsonWriter writer = new JsonTextWriter(fs)) {
                            Serializer.Serialize(writer, view.GetSaveData());
                        }
                    }
                } catch (Exception ex) {
                    Console.WriteLine(ex.StackTrace);
                    Console.WriteLine(ex.Message);

                    MessageBox.Show("Project could not be saved!");
                }
            }
        }

        public static void LoadProject<T>(ISavable<T> view, bool dialog=false, bool message=true) {
            if (dialog)
                Directory.CreateDirectory(view.DefaultSaveFolder);
            string path = dialog ? IOHelper.LoadProjectDialog(view.DefaultSaveFolder) : view.AutoSavePath;

            // If the file name is not an empty string open it for saving.  
            if (path != "") {
                try {
                    using (StreamReader fs = new StreamReader(path)) {
                        using (JsonReader reader = new JsonTextReader(fs)) {
                            view.SetSaveData(Serializer.Deserialize<T>(reader));
                        }
                    }
                } catch (Exception ex) {
                    Console.WriteLine(ex.StackTrace);
                    Console.WriteLine(ex.Message);

                    if (message)
                        MessageBox.Show("Project could not be loaded!");
                }
            }
        }

        public static bool IsSavable(object obj) {
            return obj.GetType().GetInterfaces().Any(x =>
                        x.IsGenericType &&
                        x.GetGenericTypeDefinition() == typeof(ISavable<>));
        }
    }
}
