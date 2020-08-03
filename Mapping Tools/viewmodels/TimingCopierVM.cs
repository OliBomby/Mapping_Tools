using System;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Components.Domain;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Mapping_Tools.Classes;

namespace Mapping_Tools.Viewmodels {

    public class TimingCopierVm :INotifyPropertyChanged {
        private string _importPath;
        private string _exportPath;
        private string _resnapMode;
        private int _snap1;
        private int _snap2;

        public TimingCopierVm() {
            _importPath = "";
            _exportPath = "";
            _resnapMode = "Number of beats between objects stays the same";
            _snap1 = 16;
            _snap2 = 12;

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
                    string[] paths = IOHelper.BeatmapFileDialog(true, !SettingsManager.Settings.CurrentBeatmapDefaultFolder);
                    if( paths.Length != 0 ) {
                        ExportPath = string.Join("|", paths);
                    }
                });
        }

        public string ImportPath {
            get => _importPath;
            set {
                if( _importPath == value )
                    return;
                _importPath = value;
                OnPropertyChanged();
            }
        }

        public string ExportPath {
            get => _exportPath;
            set {
                if( _exportPath == value )
                    return;
                _exportPath = value;
                OnPropertyChanged();
            }
        }

        public string ResnapMode {
            get => _resnapMode;
            set {
                if( _resnapMode == value )
                    return;
                _resnapMode = value;
                OnPropertyChanged();
            }
        }

        public int Snap1 {
            get => _snap1;
            set {
                if( _snap1 == value )
                    return;
                _snap1 = value;
                OnPropertyChanged();
            }
        }

        public int Snap2 {
            get => _snap2;
            set {
                if( _snap2 == value )
                    return;
                _snap2 = value;
                OnPropertyChanged();
            }
        }

        public CommandImplementation ImportLoadCommand { get; }
        public CommandImplementation ImportBrowseCommand { get; }
        public CommandImplementation ExportLoadCommand { get; }
        public CommandImplementation ExportBrowseCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}