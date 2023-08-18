using Mapping_Tools.Classes;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.HitsoundStuff;
using Mapping_Tools.Classes.ToolHelpers;
using Mapping_Tools.Components.Domain;
using Mapping_Tools.Views.RhythmGuide;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Viewmodels {
    public class HitsoundPreviewHelperVm : INotifyPropertyChanged
    {
        private ObservableCollection<HitsoundZone> items;
        private bool? isAllItemsSelected = false;
        private RhythmGuideWindow rhythmGuideWindow;

        public HitsoundPreviewHelperVm() {
            items = new ObservableCollection<HitsoundZone>();

            RhythmGuideCommand = new CommandImplementation(
                _ => {
                    try {
                        if (rhythmGuideWindow == null) {
                            rhythmGuideWindow = new RhythmGuideWindow();
                            rhythmGuideWindow.Closed += RhythmGuideWindowOnClosed;
                            rhythmGuideWindow.Show();
                        } else {
                            rhythmGuideWindow.Focus();
                        }
                    } catch (Exception ex) { ex.Show(); }
                });
            AddCommand = new CommandImplementation(
                _ => {
                    try {
                        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) {
                            var editor = EditorReaderStuff.GetBeatmapEditor(EditorReaderStuff.GetFullEditorReader(), out var selected);

                            if (selected.Count == 0) {
                                MessageBox.Show("Please select a hit object to fetch the coordinates.");
                                return;
                            }

                            var positions = selected.Select(o => o.Pos).Distinct();

                            foreach (Vector2 pos in positions) {
                                var newZone = new HitsoundZone {
                                    XPos = pos.X,
                                    YPos = editor.Beatmap.General["Mode"].IntValue == 3 ? -1 : pos.Y
                                };
                                Items.Add(newZone);
                            }
                        } else {
                            Items.Add(new HitsoundZone());
                        }
                    } catch (Exception ex) { ex.Show(); }
                });
            CopyCommand = new CommandImplementation(
                _ => {
                    try {
                        int initialCount = Items.Count;
                        for (int i = 0; i < initialCount; i++) {
                            if (Items[i].IsSelected) {
                                Items.Add(Items[i].Copy());
                            }
                        }
                    } catch (Exception ex) { ex.Show(); }
                });
            RemoveCommand = new CommandImplementation(
                _ => {
                    try {
                        Items.RemoveAll(o => o.IsSelected);
                    } catch (Exception ex) { ex.Show(); }
                });
        }

        private void RhythmGuideWindowOnClosed(object sender, EventArgs e) {
            rhythmGuideWindow = null;
        }

        public ObservableCollection<HitsoundZone> Items {
            get => items;
            set {
                if (items == value) return;
                items = value;
                OnPropertyChanged();
            }
        }

        [JsonIgnore]
        public bool? IsAllItemsSelected {
            get => isAllItemsSelected;
            set {
                if (isAllItemsSelected == value) return;

                isAllItemsSelected = value;

                if (isAllItemsSelected.HasValue)
                    SelectAll(isAllItemsSelected.Value, Items);

                OnPropertyChanged();
            }
        }

        private static void SelectAll(bool select, IEnumerable<HitsoundZone> models) {
            foreach (var model in models) {
                model.IsSelected = select;
            }
        }

        [JsonIgnore]
        public CommandImplementation RhythmGuideCommand { get; }
        [JsonIgnore]
        public CommandImplementation AddCommand { get; }
        [JsonIgnore]
        public CommandImplementation CopyCommand { get; }
        [JsonIgnore]
        public CommandImplementation RemoveCommand { get; }

        [JsonIgnore]
        public IEnumerable<string> SampleSets => Enum.GetNames(typeof(SampleSet));

        [JsonIgnore]
        public IEnumerable<string> Hitsounds => Enum.GetNames(typeof(Hitsound));

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
