using Mapping_Tools.Classes.BeatmapHelper;
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
using System.Windows;

namespace Mapping_Tools.Viewmodels {

    public class MetadataManagerVM :INotifyPropertyChanged {
        private string _importPath;
        private string _exportPath;

        private string _artist;
        private string _romanisedArtist;
        private string _title;
        private string _romanisedTitle;
        private string _beatmapCreator;
        private string _source;
        private string _tags;

        public MetadataManagerVM() {
            _importPath = "";
            _exportPath = "";

            ImportLoadCommand = new CommandImplementation(
                _ => {
                    string path = IOHelper.GetCurrentBeatmap();
                    if( path != "" ) {
                        ImportPath = path;
                    }
                });

            ImportBrowseCommand = new CommandImplementation(
                _ => {
                    string[] paths = IOHelper.BeatmapFileDialog(multiselect: false);
                    if( paths.Length != 0 ) {
                        ImportPath = paths[0];
                    }
                });

            ImportCommand = new CommandImplementation(
                _ => {
                    ImportFromBeatmap(ImportPath);
                });

            ExportBrowseCommand = new CommandImplementation(
                _ => {
                    string[] paths = IOHelper.BeatmapFileDialog(multiselect: true);
                    if( paths.Length != 0 ) {
                        ExportPath = string.Join("|", paths);
                    }
                });
        }

        private void ImportFromBeatmap(string importPath) {
            try {
                BeatmapEditor editor = new BeatmapEditor(importPath);
                Beatmap beatmap = editor.Beatmap;

                Artist = beatmap.Metadata["ArtistUnicode"].StringValue;
                RomanisedArtist = beatmap.Metadata["Artist"].StringValue;
                Title = beatmap.Metadata["TitleUnicode"].StringValue;
                RomanisedTitle = beatmap.Metadata["Title"].StringValue;
                BeatmapCreator = beatmap.Metadata["Creator"].StringValue;
                Source = beatmap.Metadata["Source"].StringValue;
                Tags = beatmap.Metadata["Tags"].StringValue;
            }
            catch( Exception ex ) {
                MessageBox.Show(string.Format("{0}{1}{2}", ex.Message, Environment.NewLine, ex.StackTrace), "Error");
            }
        }

        private string RemoveDuplicateTags(string tagsString) {
            string[] tags = tagsString.Split(' ');
            return string.Join(" ", new HashSet<string>(tags));
        }

        public string ImportPath {
            get { return _importPath; }
            set {
                if( _importPath == value )
                    return;
                _importPath = value;
                OnPropertyChanged();
            }
        }

        public string ExportPath {
            get { return _exportPath; }
            set {
                if( _exportPath == value )
                    return;
                _exportPath = value;
                OnPropertyChanged();
            }
        }

        public string Artist {
            get { return _artist; }
            set {
                if( _artist == value )
                    return;
                _artist = value;
                OnPropertyChanged();
            }
        }

        public string RomanisedArtist {
            get { return _romanisedArtist; }
            set {
                if( _romanisedArtist == value )
                    return;
                _romanisedArtist = value;
                OnPropertyChanged();
            }
        }

        public string Title {
            get { return _title; }
            set {
                if( _title == value )
                    return;
                _title = value;
                OnPropertyChanged();
            }
        }

        public string RomanisedTitle {
            get { return _romanisedTitle; }
            set {
                if( _romanisedTitle == value )
                    return;
                _romanisedTitle = value;
                OnPropertyChanged();
            }
        }

        public string BeatmapCreator {
            get { return _beatmapCreator; }
            set {
                if( _beatmapCreator == value )
                    return;
                _beatmapCreator = value;
                OnPropertyChanged();
            }
        }

        public string Source {
            get { return _source; }
            set {
                if( _source == value )
                    return;
                _source = value;
                OnPropertyChanged();
            }
        }

        public string Tags {
            get { return _tags; }
            set {
                if( _tags == value )
                    return;
                _tags = RemoveDuplicateTags(value);
                OnPropertyChanged();
            }
        }

        public CommandImplementation ImportLoadCommand { get; }
        public CommandImplementation ImportBrowseCommand { get; }
        public CommandImplementation ImportCommand { get; }
        public CommandImplementation ExportBrowseCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}