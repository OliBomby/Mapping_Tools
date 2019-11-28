using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Windows;

namespace Mapping_Tools.Classes.SystemTools {
    public enum ErrorType
    {
        Success,
        Error,
        Warning
    }

    public static class ProjectManager {
        private static readonly JsonSerializer Serializer = new JsonSerializer {
            NullValueHandling = NullValueHandling.Ignore,
            TypeNameHandling = TypeNameHandling.All,
            Formatting = Formatting.Indented
        };

        public static void SaveJson(string path, object obj) {
            using (StreamWriter fs = new StreamWriter(path)) {
                using (JsonTextWriter reader = new JsonTextWriter(fs)) {
                    Serializer.Serialize(reader, obj);
                }
            }
        }
        
        public static T LoadJson<T>(string path) {
            using (StreamReader fs = new StreamReader(path)) {
                using (JsonReader reader = new JsonTextReader(fs)) {
                    return Serializer.Deserialize<T>(reader);
                }
            }
        }

        public static void SaveProject<T>(ISavable<T> view, bool dialog=false) {
            if (dialog)
                Directory.CreateDirectory(view.DefaultSaveFolder);
            string path = dialog ? IOHelper.SaveProjectDialog(view.DefaultSaveFolder) : view.AutoSavePath;

            // If the file name is not an empty string open it for saving.  
            if (path == "") return;
            try {
                SaveJson(path, view.GetSaveData());
            } catch (Exception ex) {
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine(ex.Message);

                MessageBox.Show("Project could not be saved!");
            }
        }

        public static void LoadProject<T>(ISavable<T> view, bool dialog=false, bool message=true) {
            if (dialog)
                Directory.CreateDirectory(view.DefaultSaveFolder);
            string path = dialog ? IOHelper.LoadProjectDialog(view.DefaultSaveFolder) : view.AutoSavePath;

            // If the file name is not an empty string open it for saving.  
            if (path == "") return;
            try {
                view.SetSaveData(LoadJson<T>(path));
            } catch (Exception ex) {
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine(ex.Message);

                if (message)
                    MessageBox.Show("Project could not be loaded!");
            }
        }

        public static bool IsSavable(object obj) {
            return IsSavable(obj.GetType());
        }

        public static bool IsSavable(Type type) {
            return type.GetInterfaces().Any(x =>
                x.IsGenericType &&
                x.GetGenericTypeDefinition() == typeof(ISavable<>));
        }
    }
}
