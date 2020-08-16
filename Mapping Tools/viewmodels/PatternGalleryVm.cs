using Mapping_Tools.Classes;
using Mapping_Tools.Classes.HitsoundStuff;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.Tools;
using Mapping_Tools.Components.Domain;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Mapping_Tools.Classes.Tools.PatternGallery;
using Newtonsoft.Json;

namespace Mapping_Tools.Viewmodels {
    public class PatternGalleryVm : BindableBase
    {
        private ObservableCollection<OsuPattern> _patterns;
        public ObservableCollection<OsuPattern> Patterns {
            get => _patterns;
            set => Set(ref _patterns, value);
        }

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
        public CommandImplementation AddCommand { get; }
        [JsonIgnore]
        public CommandImplementation RemoveCommand { get; }

        [JsonIgnore]
        public OsuPatternFileHandler FileHandler { get; set; }

        [JsonIgnore]
        public string[] Paths { get; set; }
        [JsonIgnore]
        public bool Quick { get; set; }

        public PatternGalleryVm() {
            _patterns = new ObservableCollection<OsuPattern>();

            AddCommand = new CommandImplementation(
                _ => {
                    try {
                        var reader = EditorReaderStuff.GetFullEditorReader();
                        var editor = EditorReaderStuff.GetNewestVersion(IOHelper.GetCurrentBeatmap(), reader);
                        var patternMaker = new OsuPatternMaker();
                        var pattern = patternMaker.FromSelectedWithSave(editor.Beatmap, "test", FileHandler);
                        Patterns.Add(pattern);
                    } catch (Exception ex) { ex.Show(); }
                });
            RemoveCommand = new CommandImplementation(
                _ => {
                    try {
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
