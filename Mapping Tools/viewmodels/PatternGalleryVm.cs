using Mapping_Tools.Classes;
using Mapping_Tools.Classes.HitsoundStuff;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.Tools;
using Mapping_Tools.Components.Domain;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace Mapping_Tools.Viewmodels {
    public class PatternGalleryVm : BindableBase
    {
        private ObservableCollection<HitsoundZone> _patterns;
        private bool? _isAllItemsSelected;

        public PatternGalleryVm() {
            _patterns = new ObservableCollection<HitsoundZone>();

            AddCommand = new CommandImplementation(
                _ => {
                    try {
                        var newZone = new HitsoundZone();
                        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) {
                            var editor = EditorReaderStuff.GetBeatmapEditor(EditorReaderStuff.GetFullEditorReader(),
                                out var selected);
                            if (selected.Count > 0) {
                                newZone.XPos = selected[0].Pos.X;
                                newZone.YPos = editor.Beatmap.General["Mode"].IntValue == 3 ? -1 : selected[0].Pos.Y;
                            } else {
                                MessageBox.Show("Please select a hit object to fetch the coordinates.");
                            }
                        }
                        Patterns.Add(newZone);
                    } catch (Exception ex) { ex.Show(); }
                });
            RemoveCommand = new CommandImplementation(
                _ => {
                    try {
                        Patterns.RemoveAll(o => o.IsSelected);
                    } catch (Exception ex) { ex.Show(); }
                });
        }

        public ObservableCollection<HitsoundZone> Patterns {
            get => _patterns;
            set => Set(ref _patterns, value);
        }

        public bool? IsAllItemsSelected {
            get => _isAllItemsSelected;
            set {
                if (Set(ref _isAllItemsSelected, value)) {
                    if (_isAllItemsSelected.HasValue)
                        SelectAll(_isAllItemsSelected.Value, Patterns);
                }
            }
        }

        private static void SelectAll(bool select, IEnumerable<HitsoundZone> models) {
            foreach (var model in models) {
                model.IsSelected = select;
            }
        }

        public CommandImplementation AddCommand { get; }
        public CommandImplementation RemoveCommand { get; }
    }
}
