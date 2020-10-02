using Mapping_Tools.Classes;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.Tools;
using Mapping_Tools.Classes.Tools.PatternGallery;
using Mapping_Tools.Components.Domain;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Mapping_Tools.Components.Dialogs.CustomDialog;
using MaterialDesignThemes.Wpf;

namespace Mapping_Tools.Viewmodels {
    public class PatternGalleryVm : BindableBase {
        private string _collectionName;
        public string CollectionName {
            get => _collectionName;
            set => Set(ref _collectionName, value);
        }

        private ObservableCollection<OsuPattern> _patterns;
        public ObservableCollection<OsuPattern> Patterns {
            get => _patterns;
            set => Set(ref _patterns, value);
        }

        public OsuPatternFileHandler FileHandler { get; set; }

        private bool? _isAllItemsSelected;
        public bool? IsAllItemsSelected {
            get => _isAllItemsSelected;
            set {
                if (Set(ref _isAllItemsSelected, value)) {
                    if (_isAllItemsSelected.HasValue)
                        SelectAll(_isAllItemsSelected.Value, Patterns);
                }
            }
        }

        [JsonIgnore]
        public OsuPatternMaker OsuPatternMaker { get; set; }

        #region Export Options

        [JsonIgnore]
        public OsuPatternPlacer OsuPatternPlacer { get; set; }

        /// <summary>
        /// Extra time in millseconds around the patterns for deleting parts of the original map.
        /// </summary>
        public double Padding {
            get => OsuPatternPlacer.Padding;
            set => Set(ref OsuPatternPlacer.Padding, value);
        }

        /// <summary>
        /// Minimum time in beats necessary to separate parts of the pattern.
        /// </summary>
        public double PartingDistance {
            get => OsuPatternPlacer.PartingDistance;
            set => Set(ref OsuPatternPlacer.PartingDistance, value);
        }

        public PatternOverwriteMode PatternOverwriteMode {
            get => OsuPatternPlacer.PatternOverwriteMode;
            set => Set(ref OsuPatternPlacer.PatternOverwriteMode, value);
        }

        public TimingOverwriteMode TimingOverwriteMode {
            get => OsuPatternPlacer.TimingOverwriteMode;
            set => Set(ref OsuPatternPlacer.TimingOverwriteMode, value);
        }

        public bool IncludeHitsounds {
            get => OsuPatternPlacer.IncludeHitsounds;
            set => Set(ref OsuPatternPlacer.IncludeHitsounds, value);
        }

        public bool ScaleToNewCircleSize {
            get => OsuPatternPlacer.ScaleToNewCircleSize;
            set => Set(ref OsuPatternPlacer.ScaleToNewCircleSize, value);
        }

        public bool ScaleToNewTiming {
            get => OsuPatternPlacer.ScaleToNewTiming;
            set => Set(ref OsuPatternPlacer.ScaleToNewTiming, value);
        }

        public bool SnapToNewTiming {
            get => OsuPatternPlacer.SnapToNewTiming;
            set => Set(ref OsuPatternPlacer.SnapToNewTiming, value);
        }

        public int SnapDivisor1 {
            get => OsuPatternPlacer.SnapDivisor1;
            set => Set(ref OsuPatternPlacer.SnapDivisor1, value);
        }

        public int SnapDivisor2 {
            get => OsuPatternPlacer.SnapDivisor2;
            set => Set(ref OsuPatternPlacer.SnapDivisor2, value);
        }

        public bool FixGlobalSV {
            get => OsuPatternPlacer.FixGlobalSV;
            set => Set(ref OsuPatternPlacer.FixGlobalSV, value);
        }

        public bool FixColourHax {
            get => OsuPatternPlacer.FixColourHax;
            set => Set(ref OsuPatternPlacer.FixColourHax, value);
        }

        public bool FixStackLeniency {
            get => OsuPatternPlacer.FixStackLeniency;
            set => Set(ref OsuPatternPlacer.FixStackLeniency, value);
        }

        public bool FixTickRate {
            get => OsuPatternPlacer.FixTickRate;
            set => Set(ref OsuPatternPlacer.FixTickRate, value);
        }

        public double CustomScale {
            get => OsuPatternPlacer.CustomScale;
            set => Set(ref OsuPatternPlacer.CustomScale, value);
        }

        public double CustomRotate {
            get => OsuPatternPlacer.CustomRotate;
            set => Set(ref OsuPatternPlacer.CustomRotate, value);
        }

        #endregion

        [JsonIgnore]
        public CommandImplementation AddFileCommand { get; }
        [JsonIgnore]
        public CommandImplementation AddSelectedCommand { get; }
        [JsonIgnore]
        public CommandImplementation RemoveCommand { get; }


        [JsonIgnore]
        public string[] Paths { get; set; }
        [JsonIgnore]
        public bool Quick { get; set; }

        public PatternGalleryVm() {
            CollectionName = @"My Pattern Collection";
            _patterns = new ObservableCollection<OsuPattern>();
            FileHandler = new OsuPatternFileHandler();
            OsuPatternMaker = new OsuPatternMaker();
            OsuPatternPlacer = new OsuPatternPlacer();

            AddFileCommand = new CommandImplementation(
                async _ => {
                    try {
                        var viewModel = new PatternFileImportVm {
                            Name = $"Pattern {_patterns.Count + 1}"
                        };

                        var dialog = new CustomDialog(viewModel, 0);
                        var result = await DialogHost.Show(dialog, "RootDialog");

                        if (!(bool)result) return;

                        var pattern = OsuPatternMaker.FromFileWithSave(viewModel.FilePath, FileHandler, viewModel.Name);
                        Patterns.Add(pattern);
                    } catch (Exception ex) {
                        ex.Show();
                    }
                });
            AddSelectedCommand = new CommandImplementation(
                async _ => {
                    try {
                        var viewModel = new SelectedPatternImportVm() {
                            Name = $"Pattern {_patterns.Count + 1}"
                        };

                        var dialog = new CustomDialog(viewModel, 0);
                        var result = await DialogHost.Show(dialog, "RootDialog");

                        if (!(bool)result) return;

                        var reader = EditorReaderStuff.GetFullEditorReader();
                        var editor = EditorReaderStuff.GetNewestVersion(IOHelper.GetCurrentBeatmap(), reader);
                        var pattern = OsuPatternMaker.FromSelectedWithSave(editor.Beatmap, FileHandler, viewModel.Name);
                        Patterns.Add(pattern);
                    } catch (Exception ex) { ex.Show(); }
                });
            RemoveCommand = new CommandImplementation(
                _ => {
                    try {
                        // Remove all selected patterns and their files
                        foreach (var pattern in Patterns.Where(o => o.IsSelected)) {
                            File.Delete(FileHandler.GetPatternPath(pattern.FileName));
                        }
                        Patterns.RemoveAll(o => o.IsSelected);
                    } catch (Exception ex) { ex.Show(); }
                });
        }

        private static void SelectAll(bool select, IEnumerable<OsuPattern> patterns) {
            foreach (var model in patterns) {
                model.IsSelected = select;
            }
        }
    }
}
