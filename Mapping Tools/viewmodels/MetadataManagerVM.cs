using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Components.Domain;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Viewmodels
{
    public class MetadataManagerVM : INotifyPropertyChanged
    {
        private string _importPath;
        private string _exportPath;

        public MetadataManagerVM() {
            _importPath = "";
            _exportPath = "";

            ImportLoadCommand = new CommandImplementation(
                _ => {
                    string path = IOHelper.CurrentBeatmap();
                    if (path != "") {
                        ImportPath = path;
                    }
                });

            ImportBrowseCommand = new CommandImplementation(
                _ => {
                    string[] paths = IOHelper.BeatmapFileDialog(multiselect: false);
                    if (paths.Length != 0) {
                        ImportPath = paths[0];
                    }
                });

            ExportBrowseCommand = new CommandImplementation(
                _ => {
                    string[] paths = IOHelper.BeatmapFileDialog(multiselect: true);
                    if (paths.Length != 0) {
                        ExportPath = string.Join("|", paths);
                    }
                });
        }

        public string ImportPath {
            get { return _importPath; }
            set {
                if (_importPath == value) return;
                _importPath = value;
                OnPropertyChanged();
            }
        }

        public string ExportPath {
            get { return _exportPath; }
            set {
                if (_exportPath == value) return;
                _exportPath = value;
                OnPropertyChanged();
            }
        }


        public CommandImplementation ImportLoadCommand { get; }
        public CommandImplementation ImportBrowseCommand { get; }
        public CommandImplementation ExportBrowseCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
