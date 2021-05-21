using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Components.Domain;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using Mapping_Tools.Annotations;
using Mapping_Tools.Classes;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Viewmodels {

    public class MetadataManagerVm :INotifyPropertyChanged {
        private Visibility _beatmapFileNameOverflowErrorVisibility;

        private string _importPath;
        private string _exportPath;

        private string _artist;
        private string _romanisedArtist;
        private string _title;
        private string _romanisedTitle;
        private string _beatmapCreator;
        private string _source;
        private string _tags;

        private double _previewTime;
        private bool _useComboColours;
        private ObservableCollection<ComboColour> _comboColours;
        private ObservableCollection<SpecialColour> _specialColours;

        public MetadataManagerVm() {
            _importPath = "";
            _exportPath = "";

            _useComboColours = true;
            ComboColours = new ObservableCollection<ComboColour>();
            SpecialColours = new ObservableCollection<SpecialColour>();

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
                    var paths = IOHelper.BeatmapFileDialog(restore: !SettingsManager.Settings.CurrentBeatmapDefaultFolder);
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
                    var paths = IOHelper.BeatmapFileDialog(true, !SettingsManager.Settings.CurrentBeatmapDefaultFolder);
                    if( paths.Length != 0 ) {
                        ExportPath = string.Join("|", paths);
                    }
                });

            AddCommand = new CommandImplementation(_ => {
                if (ComboColours.Count >= 8) return;
                ComboColours.Add(ComboColours.Count > 0
                    ? new ComboColour(ComboColours[ComboColours.Count - 1].Color)
                    : new ComboColour(Colors.White));
            });

            RemoveCommand = new CommandImplementation(_ => {
                if (ComboColours.Count > 0) {
                    ComboColours.RemoveAt(ComboColours.Count - 1);
                }
            });

            AddSpecialCommand = new CommandImplementation(_ => {
                SpecialColours.Add(SpecialColours.Count > 0
                    ? new SpecialColour(SpecialColours[SpecialColours.Count - 1].Color)
                    : new SpecialColour(Colors.White));
            });

            RemoveSpecialCommand = new CommandImplementation(_ => {
                if (SpecialColours.Count > 0) {
                    SpecialColours.RemoveAt(SpecialColours.Count - 1);
                }
            });


            PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == "Artist" || e.PropertyName == "Title" || e.PropertyName == "BeatmapCreator") {
                // Update error visibility if there is an error
                string filename = Beatmap.GetFileName(Artist, Title, BeatmapCreator, "");
                BeatmapFileNameOverflowErrorVisibility = filename.Length > 255 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void ImportFromBeatmap(string importPath) {
            try {
                var editor = new BeatmapEditor(importPath);
                var beatmap = editor.Beatmap;

                Artist = beatmap.Metadata["ArtistUnicode"].Value;
                RomanisedArtist = beatmap.Metadata["Artist"].Value;
                Title = beatmap.Metadata["TitleUnicode"].Value;
                RomanisedTitle = beatmap.Metadata["Title"].Value;
                BeatmapCreator = beatmap.Metadata["Creator"].Value;
                Source = beatmap.Metadata["Source"].Value;
                Tags = beatmap.Metadata["Tags"].Value;

                PreviewTime = beatmap.General["PreviewTime"].DoubleValue;
                ComboColours = new ObservableCollection<ComboColour>(beatmap.ComboColours);
                SpecialColours.Clear();
                foreach (var specialColour in beatmap.SpecialColours) {
                    SpecialColours.Add(new SpecialColour(specialColour.Value.Color, specialColour.Key));
                }
            }
            catch( Exception ex ) {
                ex.Show();
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

        public double PreviewTime {
            get => _previewTime;
            set {
                if( Math.Abs(_previewTime - value) < Precision.DOUBLE_EPSILON )
                    return;
                _previewTime = value;
                OnPropertyChanged();
            }
        }

        public bool UseComboColours {
            get => _useComboColours;
            set {
                if( _useComboColours == value ) return;
                _useComboColours = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<ComboColour> ComboColours {
            get => _comboColours;
            set {
                if (_comboColours == value) return;
                _comboColours = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<SpecialColour> SpecialColours {
            get => _specialColours;
            set {
                if (_specialColours == value) return;
                _specialColours = value;
                OnPropertyChanged();
            }
        }

        public Visibility BeatmapFileNameOverflowErrorVisibility {
            get => _beatmapFileNameOverflowErrorVisibility;
            set {
                if (_beatmapFileNameOverflowErrorVisibility == value) return;
                _beatmapFileNameOverflowErrorVisibility = value;
                OnPropertyChanged();
            }
        }

        public CommandImplementation ImportLoadCommand { get; }
        public CommandImplementation ImportBrowseCommand { get; }
        public CommandImplementation ImportCommand { get; }
        public CommandImplementation ExportLoadCommand { get; }
        public CommandImplementation ExportBrowseCommand { get; }

        public CommandImplementation AddCommand { get; }
        public CommandImplementation RemoveCommand { get; }
        public CommandImplementation AddSpecialCommand { get; }
        public CommandImplementation RemoveSpecialCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}