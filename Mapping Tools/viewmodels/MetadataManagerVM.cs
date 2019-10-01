using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Components.Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Mapping_Tools.Annotations;

namespace Mapping_Tools.Viewmodels {

    public class MetadataManagerVm :INotifyPropertyChanged {
        private string _importPath;
        private string _exportPath;

        private string _artist;
        private string _romanisedArtist;
        private string _title;
        private string _romanisedTitle;
        private string _beatmapCreator;
        private string _source;
        private string _tags;

        public MetadataManagerVm() {
            _importPath = "";
            _exportPath = "";

            ImportLoadCommand = new CommandImplementation(
                _ => {
                    var path = IOHelper.GetCurrentBeatmap();
                    if( path != "" ) {
                        ImportPath = path;
                    }
                });

            ImportBrowseCommand = new CommandImplementation(
                _ => {
                    var paths = IOHelper.BeatmapFileDialog();
                    if( paths.Length != 0 ) {
                        ImportPath = paths[0];
                    }
                });

            ImportCommand = new CommandImplementation(
                _ => {
                    ImportFromBeatmap(ImportPath);
                });

            ExportLoadCommand = new CommandImplementation(
                _ => {
                    var path = IOHelper.GetCurrentBeatmap();
                    if (path != "") {
                        ExportPath = path;
                    }
                });

            ExportBrowseCommand = new CommandImplementation(
                _ => {
                    var paths = IOHelper.BeatmapFileDialog(true);
                    if( paths.Length != 0 ) {
                        ExportPath = string.Join("|", paths);
                    }
                });
        }

        private void ImportFromBeatmap(string importPath) {
            try {
                var editor = new BeatmapEditor(importPath);
                var beatmap = editor.Beatmap;

                Artist = beatmap.Metadata["ArtistUnicode"].StringValue;
                RomanisedArtist = beatmap.Metadata["Artist"].StringValue;
                Title = beatmap.Metadata["TitleUnicode"].StringValue;
                RomanisedTitle = beatmap.Metadata["Title"].StringValue;
                BeatmapCreator = beatmap.Metadata["Creator"].StringValue;
                Source = beatmap.Metadata["Source"].StringValue;
                Tags = beatmap.Metadata["Tags"].StringValue;
            }
            catch( Exception ex ) {
                MessageBox.Show($"{ex.Message}{Environment.NewLine}{ex.StackTrace}", "Error");
            }
        }

        private string RemoveDuplicateTags(string tagsString) {
            string[] tags = tagsString.Split(' ');
            return string.Join(" ", new HashSet<string>(tags));
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

        public string Artist {
            get => _artist;
            set {
                if( _artist == value )
                    return;
                _artist = value;
                OnPropertyChanged();
            }
        }

        public string RomanisedArtist {
            get => _romanisedArtist;
            set {
                if( _romanisedArtist == value )
                    return;
                _romanisedArtist = value;
                OnPropertyChanged();
            }
        }

        public string Title {
            get => _title;
            set {
                if( _title == value )
                    return;
                _title = value;
                OnPropertyChanged();
            }
        }

        public string RomanisedTitle {
            get => _romanisedTitle;
            set {
                if( _romanisedTitle == value )
                    return;
                _romanisedTitle = value;
                OnPropertyChanged();
            }
        }

        public string BeatmapCreator {
            get => _beatmapCreator;
            set {
                if( _beatmapCreator == value )
                    return;
                _beatmapCreator = value;
                OnPropertyChanged();
            }
        }

        public string Source {
            get => _source;
            set {
                if( _source == value )
                    return;
                _source = value;
                OnPropertyChanged();
            }
        }

        public string Tags {
            get => _tags;
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
        public CommandImplementation ExportLoadCommand { get; }
        public CommandImplementation ExportBrowseCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}