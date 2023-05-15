using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Components.Domain;
using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using Mapping_Tools.Annotations;
using Mapping_Tools.Classes;
using Mapping_Tools.Classes.MathUtil;
using System.Text.Json.Serialization;

namespace Mapping_Tools.Viewmodels {

    public class MetadataManagerVm :INotifyPropertyChanged {
        private Visibility beatmapFileNameOverflowErrorVisibility;

        private string importPath;
        private string exportPath;

        private string artist;
        private string romanisedArtist;
        private string title;
        private string romanisedTitle;
        private string beatmapCreator;
        private string source;
        private string tags;
        private bool removeDuplicateTags;
        private bool resetIds;

        private double previewTime;
        private bool useComboColours;
        private ObservableCollection<ComboColour> comboColours;
        private ObservableCollection<SpecialColour> specialColours;

        public MetadataManagerVm() {
            importPath = "";
            exportPath = "";
            removeDuplicateTags = true;

            useComboColours = true;
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
                    string importPathDirectory = Directory.GetParent(ImportPath).FullName;

                    var paths = IOHelper.BeatmapFileDialog(importPathDirectory, true);
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
            if (e.PropertyName == nameof(RomanisedArtist) || e.PropertyName == nameof(RomanisedTitle) || e.PropertyName == nameof(BeatmapCreator)) {
                // Update error visibility if there is an error
                var length = 13 + RomanisedArtist?.Length ?? 0 + RomanisedTitle?.Length ?? 0 + BeatmapCreator?.Length ?? 0;
                BeatmapFileNameOverflowErrorVisibility = length > 255 ? Visibility.Visible : Visibility.Collapsed;
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

        public string Artist {
            get => artist;
            set {
                if( artist == value )
                    return;
                artist = value;
                OnPropertyChanged();
            }
        }

        public string RomanisedArtist {
            get => romanisedArtist;
            set {
                if( romanisedArtist == value )
                    return;
                romanisedArtist = value;
                OnPropertyChanged();
            }
        }

        public string Title {
            get => title;
            set {
                if( title == value )
                    return;
                title = value;
                OnPropertyChanged();
            }
        }

        public string RomanisedTitle {
            get => romanisedTitle;
            set {
                if( romanisedTitle == value )
                    return;
                romanisedTitle = value;
                OnPropertyChanged();
            }
        }

        public string BeatmapCreator {
            get => beatmapCreator;
            set {
                if( beatmapCreator == value )
                    return;
                beatmapCreator = value;
                OnPropertyChanged();
            }
        }

        public string Source {
            get => source;
            set {
                if( source == value )
                    return;
                source = value;
                OnPropertyChanged();
            }
        }

        public string Tags {
            get => tags;
            set {
                if( tags == value )
                    return;
                tags = value;
                if (removeDuplicateTags)
                    tags = RemoveDuplicateTags(value);
                TagsOverflowErrorVisibility = tags.Length > 1024 || tags.Split(' ').Length > 100 ? Visibility.Visible : Visibility.Collapsed;
                OnPropertyChanged(nameof(TagsOverflowErrorVisibility));
                OnPropertyChanged();
            }
        }

        public bool DoRemoveDuplicateTags {
            get => removeDuplicateTags;
            set {
                if( removeDuplicateTags == value )
                    return;
                removeDuplicateTags = value;
                if (removeDuplicateTags)
                    Tags = RemoveDuplicateTags(Tags);
                OnPropertyChanged();
            }
        }

        public bool ResetIds {
            get => resetIds;
            set {
                if( resetIds == value ) return;
                resetIds = value;
                OnPropertyChanged();
            }
        }

        public double PreviewTime {
            get => previewTime;
            set {
                if( Math.Abs(previewTime - value) < Precision.DoubleEpsilon )
                    return;
                previewTime = value;
                OnPropertyChanged();
            }
        }

        public bool UseComboColours {
            get => useComboColours;
            set {
                if( useComboColours == value ) return;
                useComboColours = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<ComboColour> ComboColours {
            get => comboColours;
            set {
                if (comboColours == value) return;
                comboColours = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<SpecialColour> SpecialColours {
            get => specialColours;
            set {
                if (specialColours == value) return;
                specialColours = value;
                OnPropertyChanged();
            }
        }

        [JsonIgnore]
        public Visibility BeatmapFileNameOverflowErrorVisibility {
            get => beatmapFileNameOverflowErrorVisibility;
            set {
                if (beatmapFileNameOverflowErrorVisibility == value) return;
                beatmapFileNameOverflowErrorVisibility = value;
                OnPropertyChanged();
            }
        }

        [JsonIgnore]
        public Visibility TagsOverflowErrorVisibility { get; set; }

        [JsonIgnore]
        public CommandImplementation ImportLoadCommand { get; }
        [JsonIgnore]
        public CommandImplementation ImportBrowseCommand { get; }
        [JsonIgnore]
        public CommandImplementation ImportCommand { get; }
        [JsonIgnore]
        public CommandImplementation ExportLoadCommand { get; }
        [JsonIgnore]
        public CommandImplementation ExportBrowseCommand { get; }

        [JsonIgnore]
        public CommandImplementation AddCommand { get; }
        [JsonIgnore]
        public CommandImplementation RemoveCommand { get; }
        [JsonIgnore]
        public CommandImplementation AddSpecialCommand { get; }
        [JsonIgnore]
        public CommandImplementation RemoveSpecialCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}