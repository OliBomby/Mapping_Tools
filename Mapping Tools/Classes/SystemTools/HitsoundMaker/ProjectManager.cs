using Mapping_Tools.Classes.HitsoundStuff;
using Mapping_Tools.ViewSettings;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Mapping_Tools.Classes.SystemTools.HitsoundMaker {
    public class ProjectManager {
        public List<string> ProjectNames = new List<string>();
        public Project CurrentProject;
        private static readonly JsonSerializer Serializer = new JsonSerializer {
            NullValueHandling = NullValueHandling.Ignore
        };

        public ProjectManager() {
            GetProjects();
        }

        public void GetProjects() {
            try {
                ProjectNames = Directory.GetDirectories(MainWindow.AppWindow.HSProjectPath)
                                            .Select(Path.GetFileName).ToList();
            }
            catch( Exception ) {
                MessageBox.Show("Projects could not be loaded!");
            }
        }

        public void LoadProject(string name, HitsoundMakerSettings settings) {
            CurrentProject = new Project(name, settings);
            LoadFromJSON(CurrentProject.GetJSONPath());
        }

        public void CreateProject(string name) {
            try {
                CurrentProject = new Project(name);

                Directory.CreateDirectory(CurrentProject.GetProjectPath());
                Directory.CreateDirectory(CurrentProject.GetExportPath());
                Directory.CreateDirectory(CurrentProject.GetSamplePath());
                
                string jsonPath = CurrentProject.GetJSONPath();

                if( !File.Exists(jsonPath) ) {
                    CreateJSON(jsonPath);
                }
            }
            catch( Exception ex ) {
                MessageBox.Show(ex.Message);
            }
        }

        public void SaveProject(bool doLoading) {
            if( CurrentProject != null ) {
                try {
                    string jsonPath = CurrentProject.GetJSONPath();
                    WriteToJSON(jsonPath, doLoading);
                }
                catch( Exception ex ) {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        public void WriteToJSON(string jsonPath, bool doLoading) {
            try {
                using( StreamWriter sw = new StreamWriter(jsonPath) )
                using( JsonWriter writer = new JsonTextWriter(sw) ) {
                    Serializer.Serialize(writer, CurrentProject);
                }
            }
            catch( Exception ex ) {
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine(ex.Message);

                MessageBox.Show("Project could not be saved!");
            }

            if( doLoading ) {
                LoadFromJSON(jsonPath);
            }
        }

        private void LoadFromJSON(string jsonPath) {
            try {
                using( StreamReader sr = new StreamReader(jsonPath) )
                using( JsonReader reader = new JsonTextReader(sr) ) {
                    CurrentProject = Serializer.Deserialize<Project>(reader);
                }
            }
            catch( Exception ex ) {
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine(ex.Message);

                MessageBox.Show("Project could not be loaded!");
            }
        }

        private void CreateJSON(string jsonPath) {
            try {
                using( StreamWriter sw = new StreamWriter(jsonPath) )
                using( JsonWriter writer = new JsonTextWriter(sw) ) {
                    Serializer.Serialize(writer, CurrentProject);
                }
            }
            catch( Exception ex ) {
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine(ex.Message);

                MessageBox.Show("Project could not be created!");
            }
        }
    }
}
