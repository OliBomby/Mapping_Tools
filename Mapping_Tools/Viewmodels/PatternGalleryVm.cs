using Mapping_Tools.Classes;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.BeatmapHelper.BeatDivisors;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.ToolHelpers;
using Mapping_Tools.Classes.Tools.PatternGallery;
using Mapping_Tools.Components.Dialogs.CustomDialog;
using Mapping_Tools.Components.Domain;
using MaterialDesignThemes.Wpf;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
// ReSharper disable AsyncVoidLambda

namespace Mapping_Tools.Viewmodels {
    public class PatternGalleryVm : BindableBase {
        private string collectionName;
        public string CollectionName {
            get => collectionName;
            set => Set(ref collectionName, value);
        }

        private ObservableCollection<OsuPattern> patterns;
        public ObservableCollection<OsuPattern> Patterns {
            get => patterns;
            set => Set(ref patterns, value);
        }

        public OsuPatternFileHandler FileHandler { get; set; }

        [JsonIgnore]
        public OsuPatternMaker OsuPatternMaker { get; set; }

        [JsonIgnore]
        public OsuPatternPlacer OsuPatternPlacer { get; set; }

        [JsonIgnore]
        private readonly ListCollectionView patternCollectionView;

        [JsonIgnore]
        private string searchFilter = string.Empty;
        [JsonIgnore]
        public string SearchFilter { get => searchFilter; set => SetSearchFilter(value); }

        [JsonIgnore]
        public string[] SortableProperties { get; } = { "Name", "Creation time", "Last used time", "Usage count", "Object count", "Duration", "Beat length" };

        [JsonIgnore]
        private string sortProperty = "Creation time";
        [JsonIgnore]
        public string SortProperty { get => sortProperty; set => SetSortProperty(value); }

        [JsonIgnore]
        private int sortDirection;
        [JsonIgnore]
        public int SortDirection { get => sortDirection; set => SetSortDirection(value); }

        [JsonIgnore]
        public ObservableCollection<FrameworkElement> PatternGroupContextMenu { get; }

        #region Options

        private ExportTimeMode exportTimeMode;
        public ExportTimeMode ExportTimeMode {
            get => exportTimeMode;
            set {
                if (Set(ref exportTimeMode, value)) {
                    RaisePropertyChanged(nameof(CustomExportTimeVisible));
                }
            }
        }

        [JsonIgnore]
        public IEnumerable<ExportTimeMode> ExportTimeModes =>
            Enum.GetValues(typeof(ExportTimeMode)).Cast<ExportTimeMode>();

        private double customExportTime;
        public double CustomExportTime {
            get => customExportTime;
            set => Set(ref customExportTime, value);
        }

        [JsonIgnore]
        public bool CustomExportTimeVisible => ExportTimeMode == ExportTimeMode.Custom;

        /// <summary>
        /// Extra time in milliseconds around the patterns for deleting parts of the original map.
        /// </summary>
        public double Padding {
            get => OsuPatternPlacer.Padding;
            set {
                if (Set(ref OsuPatternPlacer.Padding, value)) {
                    OsuPatternMaker.Padding = value;
                }
            }
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

        [JsonIgnore]
        public IEnumerable<PatternOverwriteMode> PatternOverwriteModes =>
            Enum.GetValues(typeof(PatternOverwriteMode)).Cast<PatternOverwriteMode>();

        public TimingOverwriteMode TimingOverwriteMode {
            get => OsuPatternPlacer.TimingOverwriteMode;
            set => Set(ref OsuPatternPlacer.TimingOverwriteMode, value);
        }

        [JsonIgnore]
        public IEnumerable<TimingOverwriteMode> TimingOverwriteModes =>
            Enum.GetValues(typeof(TimingOverwriteMode)).Cast<TimingOverwriteMode>();

        public bool IncludeHitsounds {
            get => OsuPatternPlacer.IncludeHitsounds;
            set => Set(ref OsuPatternPlacer.IncludeHitsounds, value);
        }

        public bool IncludeKiai {
            get => OsuPatternPlacer.IncludeKiai;
            set => Set(ref OsuPatternPlacer.IncludeKiai, value);
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

        public IBeatDivisor[] BeatDivisors {
            get => OsuPatternPlacer.BeatDivisors;
            set => Set(ref OsuPatternPlacer.BeatDivisors, value);
        }

        public bool FixGlobalSv {
            get => OsuPatternPlacer.FixGlobalSv;
            set => Set(ref OsuPatternPlacer.FixGlobalSv, value);
        }

        public bool FixBpmSv {
            get => OsuPatternPlacer.FixBpmSv;
            set => Set(ref OsuPatternPlacer.FixBpmSv, value);
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
            get => MathHelper.RadiansToDegrees(OsuPatternPlacer.CustomRotate);
            set => Set(ref OsuPatternPlacer.CustomRotate, MathHelper.DegreesToRadians(value));
        }

        #endregion

        [JsonIgnore]
        public CommandImplementation AddCodeCommand { get; }
        [JsonIgnore]
        public CommandImplementation AddFileCommand { get; }
        [JsonIgnore]
        public CommandImplementation AddSelectedCommand { get; }
        [JsonIgnore]
        public CommandImplementation RemoveCommand { get; }
        [JsonIgnore]
        public CommandImplementation OpenExplorerSelectedCommand { get; }
        [JsonIgnore]
        public CommandImplementation ShowDetailsCommand { get; }

        [JsonIgnore]
        public string[] Paths { get; set; }
        [JsonIgnore]
        public bool Quick { get; set; }

        public PatternGalleryVm() {
            CollectionName = @"My Pattern Collection";
            patterns = new ObservableCollection<OsuPattern>();
            FileHandler = new OsuPatternFileHandler();
            OsuPatternMaker = new OsuPatternMaker();
            OsuPatternPlacer = new OsuPatternPlacer();

            ExportTimeMode = ExportTimeMode.Current;
            CustomExportTime = 0;

            // Set up filters
            patternCollectionView = (ListCollectionView) CollectionViewSource.GetDefaultView(Patterns);
            patternCollectionView.Filter = PatternNameFilter;
            patternCollectionView.CustomSort = new CustomPatternSorter { Parent = this };
            patternCollectionView.GroupDescriptions?.Add(new PropertyGroupDescription(nameof(OsuPattern.Group)));

            PatternGroupContextMenu = new ObservableCollection<FrameworkElement>();
            patterns.CollectionChanged += (_, _) => UpdateGroupContextMenuItems();

            AddCodeCommand = new CommandImplementation(
                async _ => {
                    try {
                        var viewModel = new PatternCodeImportVm {
                            Name = $"Pattern {patterns.Count + 1}"
                        };

                        var dialog = new CustomDialog(viewModel, 0);
                        var result = await DialogHost.Show(dialog, "RootDialog");

                        if (result is not true) return;

                        var hitObjects = new List<HitObject>();
                        foreach (string o in Regex.Split(viewModel.HitObjects, Environment.NewLine)) {
                            try {
                                hitObjects.Add(new HitObject(o.Trim()));
                            } catch (Exception ex) { Console.WriteLine(ex);}
                        }
                        var timingPoints = new List<TimingPoint>();
                        foreach (string o in Regex.Split(viewModel.TimingPoints, Environment.NewLine)) {
                            try {
                                timingPoints.Add(new TimingPoint(o.Trim()));
                            } catch (Exception ex) { Console.WriteLine(ex);}
                        }

                        // The pattern needs at least one hitobject
                        if (hitObjects.Count == 0) {
                            MessageBox.Show("At least one valid hit object is required.");
                            return;
                        }

                        var pattern = OsuPatternMaker.FromObjectsWithSave(
                            hitObjects, timingPoints, FileHandler, viewModel.Name, null, viewModel.GlobalSv, viewModel.GameMode);
                        Patterns.Add(pattern);
                    } catch (Exception ex) {
                        ex.Show();
                    }
                });
            AddFileCommand = new CommandImplementation(
                async _ => {
                    try {
                        var viewModel = new PatternFileImportVm {
                            Name = $"Pattern {patterns.Count + 1}"
                        };

                        var dialog = new CustomDialog(viewModel, 0);
                        var result = await DialogHost.Show(dialog, "RootDialog");

                        if (result is not true) return;

                        var pattern = OsuPatternMaker.FromFileWithSave(
                            viewModel.FilePath, FileHandler, viewModel.Name, viewModel.Filter, viewModel.StartTime, viewModel.EndTime);
                        Patterns.Add(pattern);
                    } catch (Exception ex) {
                        ex.Show();
                    }
                });
            AddSelectedCommand = new CommandImplementation(
                async _ => {
                    try {
                        var viewModel = new SelectedPatternImportVm() {
                            Name = $"Pattern {patterns.Count + 1}"
                        };

                        var dialog = new CustomDialog(viewModel, 0);
                        var result = await DialogHost.Show(dialog, "RootDialog");

                        if (result is not true) return;

                        var reader = EditorReaderStuff.GetFullEditorReader();
                        var editor = EditorReaderStuff.GetNewestVersion(IOHelper.GetCurrentBeatmap(), reader);
                        var pattern = OsuPatternMaker.FromSelectedWithSave(editor.Beatmap, FileHandler, viewModel.Name);
                        Patterns.Add(pattern);
                    } catch (Exception ex) { ex.Show(); }
                });
            RemoveCommand = new CommandImplementation(
                _ => {
                    try {
                        var selected = Patterns.Where(o => o.IsSelected).ToList();
                        if (selected.Count == 0) return;

                        if (!(Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))){
                            string message = selected.Count == 1 ? $"Are you sure you want to delete \"{selected.First().Name}\"?" :
                                selected.Count == 2 ? $"Are you sure you want to delete \"{selected[0].Name}\" and \"{selected[1].Name}\"?" :
                                $"Are you sure you want to delete \"{selected[0].Name}\" and {selected.Count-1} others?";
                            var messageBoxResult = MessageBox.Show(message, "Confirm deletion", MessageBoxButton.YesNo);
                            if (messageBoxResult != MessageBoxResult.Yes) return;
                        }

                        // Remove all selected patterns and their files
                        foreach (var pattern in Patterns.Where(o => o.IsSelected)) {
                            File.Delete(FileHandler.GetPatternPath(pattern.FileName));
                        }
                        Patterns.RemoveAll(o => o.IsSelected);
                    } catch (Exception ex) { ex.Show(); }
                });
            OpenExplorerSelectedCommand = new CommandImplementation(
                _ => {
                    try {
                        ShowSelectedInExplorer.FilesOrFolders(
                            Patterns.Where(o => o.IsSelected).Select(o => FileHandler.GetPatternPath(o.FileName)));
                    } catch (Exception ex) { ex.Show(); }
                });
            ShowDetailsCommand = new CommandImplementation(
                async _ => {
                    try {
                        var pattern = Patterns.FirstOrDefault(o => o.IsSelected);
                        if (pattern is null) return;

                        var viewModel = new OsuPatternDetailsVm(pattern);
                        var dialog = new CustomDialog(viewModel, 0);
                        var result = await DialogHost.Show(dialog, "RootDialog");

                        if (result is not true) return;

                        pattern.Name = viewModel.Name;
                    } catch (Exception ex) { ex.Show(); }
                });
        }

        private void UpdateGroupContextMenuItems() {
            var groups = patterns.Select(o => o.Group).Distinct().ToList();
            groups.Sort(StringComparer.Ordinal);

            var existingGroups = PatternGroupContextMenu.OfType<MenuItem>().SkipLast(1).Select(o => {
                var header = o.Header as string;
                return header == "None" ? null : header;
            }).ToList();
            existingGroups.Sort(StringComparer.Ordinal);

            if (existingGroups.SequenceEqual(groups))
                return;

            PatternGroupContextMenu.Clear();

            foreach (var group in groups) {
                var item = new MenuItem {
                    Header = string.IsNullOrEmpty(group) ? "None" : group,
                    Command = new CommandImplementation(
                        _ => {
                            try {
                                foreach (var pattern in Patterns.Where(o => o.IsSelected)) {
                                    pattern.Group = group;
                                }

                                patternCollectionView.Refresh();
                                UpdateGroupContextMenuItems();
                            } catch (Exception ex) { ex.Show(); }
                        })
                };
                PatternGroupContextMenu.Add(item);
            }

            PatternGroupContextMenu.Add(new Separator());

            var typeNewGroupItem = new MenuItem {
                Header = "Type new group name...",
                Command = new CommandImplementation(
                    async _ => {
                        try {
                            var viewModel = new NewGroupVm { GroupName = $"Group {groups.Count}" };
                            var dialog = new CustomDialog(viewModel, 0);
                            var result = await DialogHost.Show(dialog, "RootDialog");

                            if (result is not true) return;

                            foreach (var pattern in Patterns.Where(o => o.IsSelected)) {
                                pattern.Group = viewModel.GroupName;
                            }

                            patternCollectionView.Refresh();
                            UpdateGroupContextMenuItems();
                        } catch (Exception ex) { ex.Show(); }
                    })
            };

            var renameGroupItem = new MenuItem {
                Header = "_Rename group...",
                Command = new CommandImplementation(
                    async _ => {
                        try {
                            var viewModel = new NewGroupVm { GroupName = $"Group {groups.Count}" };
                            var dialog = new CustomDialog(viewModel, 0);
                            var result = await DialogHost.Show(dialog, "RootDialog");

                            if (result is not true) return;

                            string group = Patterns.FirstOrDefault(o => o.IsSelected)?.Group;
                            foreach (var pattern in Patterns.Where(o => o.Group == group)) {
                                pattern.Group = viewModel.GroupName;
                            }

                            patternCollectionView.Refresh();
                            UpdateGroupContextMenuItems();
                        } catch (Exception ex) {
                            ex.Show();
                        }
                    })
            };

            PatternGroupContextMenu.Add(renameGroupItem);
            PatternGroupContextMenu.Add(typeNewGroupItem);
        }

        public void SetSelectAll(bool select) {
            foreach (var model in Patterns) {
                model.IsSelected = select;
            }
        }


        #region UI helpers

        private bool PatternNameFilter(object item) {
            return string.IsNullOrEmpty(SearchFilter) || ((OsuPattern)item).Name.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase);
        }

        private void SetSearchFilter(string value) {
            searchFilter = value;
            patternCollectionView.Refresh();
        }

        private void SetSortProperty(string value) {
            sortProperty = value;
            patternCollectionView.Refresh();
        }

        private void SetSortDirection(int value) {
            sortDirection = value;
            patternCollectionView.Refresh();
        }

        private class CustomPatternSorter : IComparer {
            public PatternGalleryVm Parent { get; init; }

            public int Compare(object x, object y) {
                if (x is not OsuPattern p1 || y is not OsuPattern p2) return -1;

                int result = Parent.SortProperty switch {
                    "Name" => string.Compare(p1.Name, p2.Name, StringComparison.Ordinal),
                    "Creation time" => DateTime.Compare(p1.CreationTime, p2.CreationTime),
                    "Last used time" => DateTime.Compare(p1.LastUsedTime, p2.LastUsedTime),
                    "Usage count" => p1.UseCount.CompareTo(p2.UseCount),
                    "Object count" => p1.ObjectCount.CompareTo(p2.ObjectCount),
                    "Duration" => TimeSpan.Compare(p1.Duration, p2.Duration),
                    "Beat length" => p1.BeatLength.CompareTo(p2.BeatLength),
                    _ => -1
                };

                return Parent.SortDirection == 0 ? result : -result;
            }
        }

        #endregion
    }
}
