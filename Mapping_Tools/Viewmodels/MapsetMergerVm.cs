using Mapping_Tools.Classes;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Components.Domain;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;

namespace Mapping_Tools.Viewmodels {

    public class MapsetMergerVm : BindableBase {
        private string exportPath;
        public string ExportPath {
            get => exportPath; 
            set => Set(ref exportPath, value);
        }

        private bool moveSbToBeatmap;
        public bool MoveSbToBeatmap {
            get => moveSbToBeatmap;
            set => Set(ref moveSbToBeatmap, value);
        }

        public ObservableCollection<MapsetItem> Mapsets { get; }

        public MapsetMergerVm() {
            Mapsets = new ObservableCollection<MapsetItem>();
            ExportPath = MainWindow.ExportPath;

            AddMapsetCommand = new CommandImplementation(_ => {
                string path = MainWindow.AppWindow.GetCurrentMaps().FirstOrDefault() ?? string.Empty;

                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) {
                    try {
                        path = IOHelper.GetCurrentBeatmap();
                    } catch (Exception ex) {
                        ex.Show();
                    }
                }

                var dir = Directory.GetParent(path);
                string folderPath = dir.FullName;
                string name = dir.Name;

                Mapsets.Add(new MapsetItem { Name = name, Path = folderPath });
            });

            RemoveMapsetCommand = new CommandImplementation(_ => {
                if (Mapsets.Any(o => o.IsSelected)) {
                    Mapsets.RemoveAll(o => o.IsSelected);
                    return;
                }
                if (Mapsets.Count > 0) {
                    Mapsets.RemoveAt(Mapsets.Count - 1);
                }
            });

            BrowseExportPathCommand = new CommandImplementation(_ => {
                try {
                    string path = IOHelper.FolderDialog();
                    if (path != "") {
                        ExportPath = path;
                    }
                } catch (Exception ex) { ex.Show(); }
            });
        }

        [JsonIgnore]
        public CommandImplementation AddMapsetCommand { get; }
        [JsonIgnore]
        public CommandImplementation RemoveMapsetCommand { get; }
        [JsonIgnore]
        public CommandImplementation BrowseExportPathCommand { get; }

        public class MapsetItem : BindableBase {
            private bool isSelected;
            [JsonIgnore]
            public bool IsSelected {
                get => isSelected;
                set => Set(ref isSelected, value);
            }

            private string name;
            public string Name {
                get => name;
                set => Set(ref name, value);
            }

            private string path;
            public string Path {
                get => path;
                set => Set(ref path, value);
            }

            public MapsetItem() {
                BrowseCommand = new CommandImplementation(_ => {
                    try {
                        string path = IOHelper.FolderDialog();
                        if (path != "") {
                            Path = path;
                        }
                    } catch (Exception ex) { ex.Show(); }
                });
            }

            [JsonIgnore]
            public CommandImplementation BrowseCommand { get; }
        }
    }
}