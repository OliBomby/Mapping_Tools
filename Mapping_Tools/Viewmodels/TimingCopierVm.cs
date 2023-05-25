using System;
using System.IO;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Components.Domain;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Mapping_Tools.Classes;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.BeatmapHelper.BeatDivisors;
using System.Text.Json.Serialization;

namespace Mapping_Tools.Viewmodels {

    public class TimingCopierVm :INotifyPropertyChanged {
        private string importPath;
        private string exportPath;
        private string resnapMode;
        private IBeatDivisor[] beatDivisors;

        public TimingCopierVm() {
            importPath = "";
            exportPath = "";
            resnapMode = "Number of beats between objects stays the same";
            beatDivisors = RationalBeatDivisor.GetDefaultBeatDivisors();

            ImportLoadCommand = new CommandImplementation(
                _ => {
                    try {
                        string path = IOHelper.GetCurrentBeatmap();
                        if (path != "") {
                            ImportPath = path;
                        }
                    } catch (Exception ex) {
                        ex.Show();
                    }
                });

            ImportBrowseCommand = new CommandImplementation(
                _ => {
                    string[] paths = IOHelper.BeatmapFileDialog(restore: !SettingsManager.Settings.CurrentBeatmapDefaultFolder);
                    if( paths.Length != 0 ) {
                        ImportPath = paths[0];
                    }
                });

            ExportLoadCommand = new CommandImplementation(
                _ => {
                    try {
                        string path = IOHelper.GetCurrentBeatmap();
                        if (path != "") {
                            ExportPath = path;
                        }
                    } catch (Exception ex) {
                        ex.Show();
                    }
                });

            ExportBrowseCommand = new CommandImplementation(
                _ => {
                    string importPathDirectory = Directory.GetParent(ImportPath).FullName;
                    
                    string[] paths = IOHelper.BeatmapFileDialog(importPathDirectory, true);
                    if ( paths.Length != 0 ) {
                        ExportPath = string.Join("|", paths);
                    }
                });
        }

        public string ImportPath {
            get => importPath;
            set {
                if( importPath == value )
                    return;
                importPath = value;
                OnPropertyChanged();
            }
        }

        public string ExportPath {
            get => exportPath;
            set {
                if( exportPath == value )
                    return;
                exportPath = value;
                OnPropertyChanged();
            }
        }

        public string ResnapMode {
            get => resnapMode;
            set {
                if( resnapMode == value )
                    return;
                resnapMode = value;
                OnPropertyChanged();
            }
        }

        public IBeatDivisor[] BeatDivisors {
            get => beatDivisors;
            set {
                if( beatDivisors == value )
                    return;
                beatDivisors = value;
                OnPropertyChanged();
            }
        }

        [JsonIgnore]
        public CommandImplementation ImportLoadCommand { get; }
        [JsonIgnore]
        public CommandImplementation ImportBrowseCommand { get; }
        [JsonIgnore]
        public CommandImplementation ExportLoadCommand { get; }
        [JsonIgnore]
        public CommandImplementation ExportBrowseCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}