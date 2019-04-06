using Mapping_Tools.Classes.HitsoundStuff;
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
        public List<String> ProjectPaths = new List<string>();
        public Project CurrentProject;
        private static readonly JsonSerializer Serializer = new JsonSerializer {
            NullValueHandling = NullValueHandling.Ignore
        };

        public ProjectManager() {
            GetProjects();
        }

        public void GetProjects() {
            try {
                String[] str = Directory.GetDirectories(MainWindow.AppWindow.HSProjectPath)
                                            .Select(Path.GetFileName)
                                            .ToArray<String>();

                foreach( String folder in str ) {
                    ProjectPaths.Add(Path.Combine(MainWindow.AppWindow.HSProjectPath, folder));
                }
            }
            catch( Exception e ) {
                MessageBox.Show("Projects could not be loaded!");
            }
        }

        public void LoadProject(string baseBeatmap, Sample defaultSample, ObservableCollection<HitsoundLayer> hitsoundLayers, String projectpath) {
            CurrentProject = new Project(baseBeatmap, defaultSample, hitsoundLayers, projectpath);
            LoadFromJSON(Path.Combine(projectpath, "data.json"));
        }

        public void CreateProject(String name) {
            try {
                String path = Path.Combine(MainWindow.AppWindow.HSProjectPath, name);
                Directory.CreateDirectory(path);
                Directory.CreateDirectory(Path.Combine(path, "Exports"));
                Directory.CreateDirectory(Path.Combine(path, "Samples"));

                CurrentProject = new Project(path);

                String jsonPath = Path.Combine(path, "data.json");

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
                    String jsonPath = Path.Combine(CurrentProject.ProjectPath, "data.json");
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

        private void LoadFromJSON(String jsonPath) {
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

        private void CreateJSON(String jsonPath) {
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
