using System;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Components.Domain;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Mapping_Tools.Classes;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.BeatmapHelper.BeatDivisors;

namespace Mapping_Tools.Viewmodels {

    public class TimingCopierVm :INotifyPropertyChanged {
        private string _importPath;
        private string _exportPath;
        private string _resnapMode;
        private IBeatDivisor[] _beatDivisors;

        public TimingCopierVm() {
            _importPath = "";
            _exportPath = "";
            _resnapMode = "Number of beats between objects stays the same";
            _beatDivisors = RationalBeatDivisor.GetDefaultBeatDivisors();

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

        public IBeatDivisor[] BeatDivisors {
            get => _beatDivisors;
            set {
                if( _beatDivisors == value )
                    return;
                _beatDivisors = value;
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