using System;
using System.IO;
using System.Linq;
using System.Windows;
using Mapping_Tools.Application.Projects;
using Mapping_Tools.Infrastructure.Projects;

namespace Mapping_Tools.Classes.SystemTools {
    public enum ErrorType
    {
        Success,
        Error,
        Warning
    }

    public static class ProjectManager {
        private static readonly IProjectSerializer Serializer =
            new LegacyProjectJsonSerializer();
        private static readonly IProjectStore Store =
            new FileSystemProjectStore(Serializer);

        public static void WriteJson(StreamWriter streamWriter, object obj) {
            streamWriter.Write(Serializer.Serialize(obj));
        }

        public static void SaveJson(string path, object obj) {
            Store.SaveAsync(path, obj).GetAwaiter().GetResult();
        }
        
        public static T LoadJson<T>(string path) {
            return Store.LoadAsync<T>(path).GetAwaiter().GetResult();
        }

        public static T LoadJson<T>(Stream stream) {
            using StreamReader reader = new(stream);
            return Serializer.Deserialize<T>(reader.ReadToEnd());
        }

        public static void AutoSaveProject<T>(ISavable<T> view) {
            string path = view.AutoSavePath;
            SaveProject(view, path);

            if (view is IHasExtraAutoSaveTarget hasExtraAutoSaveTarget) {
                SaveProject(view, hasExtraAutoSaveTarget.ExtraAutoSavePath);
            }
        }

        public static void SaveProjectDialog<T>(ISavable<T> view) {
            Directory.CreateDirectory(view.DefaultSaveFolder);
            string path = IOHelper.SaveProjectDialog(view.DefaultSaveFolder);
            SaveProject(view, path);
        }

        public static void SaveProject<T>(ISavable<T> view, string path) {
            // If the file name is not an empty string open it for saving.
            if (string.IsNullOrEmpty(path)) return;
            try {
                SaveJson(path, view.GetSaveData());
            } catch (Exception ex) {
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine(ex.Message);

                MessageBox.Show("Project could not be saved!");
                ex.Show();
            }
        }

        public static void LoadProject<T>(ISavable<T> view, bool dialog=false, bool message=true) {
            if (dialog)
                Directory.CreateDirectory(view.DefaultSaveFolder);
            string path = dialog ? IOHelper.LoadProjectDialog(view.DefaultSaveFolder) : view.AutoSavePath;

            // If the file name is not an empty string open it for saving.  
            if (path == "") return;
            try {
                T project = LoadJson<T>(path);

                if (project == null) {
                    throw new Exception("Loaded project is a null reference.");
                }

                view.SetSaveData(project);
            } catch (Exception ex) {
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine(ex.Message);

                if (message) {
                    MessageBox.Show("Project could not be loaded!");
                    ex.Show();
                }
            }
        }

        public static void NewProject<T>(ISavable<T> view, bool dialog = false, bool message = true) {
            if (dialog) {
                var messageBoxResult = MessageBox.Show("Are you sure you want to start a new project? All unsaved progress will be lost.", "Confirm new project", MessageBoxButton.YesNo);
                if (messageBoxResult != MessageBoxResult.Yes) return;
            }

            try {
                T project = Activator.CreateInstance<T>();
                view.SetSaveData(project);
            } catch (Exception ex) {
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine(ex.Message);

                if (message) {
                    MessageBox.Show("New project could not be initialized!");
                    ex.Show();
                }
            }
        }

        /// <summary>
        /// Gets the project file for a savable tool with optional dialog.
        /// Uses default save path if no dialog is used.
        /// </summary>
        /// <typeparam name="T">The type of the project data</typeparam>
        /// <param name="view">The tool to get the project from</param>
        /// <param name="dialog">Whether to use a dialog</param>
        /// <returns></returns>
        public static T GetProject<T>(ISavable<T> view, bool dialog=false) {
            if (dialog)
                Directory.CreateDirectory(view.DefaultSaveFolder);
            string path = dialog ? IOHelper.LoadProjectDialog(view.DefaultSaveFolder) : view.AutoSavePath;

            return LoadJson<T>(path);
        }

        public static void SaveToolFile<T, T2>(ISavable<T> view, T2 obj, bool dialog = false) {
            if (dialog)
                Directory.CreateDirectory(view.DefaultSaveFolder);
            string path = dialog ? IOHelper.SaveProjectDialog(view.DefaultSaveFolder) : view.AutoSavePath;

            SaveJson(path, obj);
        }

        public static T2 LoadToolFile<T, T2>(ISavable<T> view, bool dialog = false) {
            if (dialog)
                Directory.CreateDirectory(view.DefaultSaveFolder);
            string path = dialog ? IOHelper.LoadProjectDialog(view.DefaultSaveFolder) : view.AutoSavePath;

            return LoadJson<T2>(path);
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
