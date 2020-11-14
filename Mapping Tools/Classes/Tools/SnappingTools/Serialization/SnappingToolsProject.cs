using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Mapping_Tools.Annotations;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators;
using Newtonsoft.Json;

namespace Mapping_Tools.Classes.Tools.SnappingTools.Serialization {
    public class SnappingToolsProject : BindableBase, IDisposable {
        [CanBeNull]
        [JsonIgnore]
        private IEnumerable<RelevantObjectsGenerator> _generators;

        private SnappingToolsPreferences _currentPreferences;
        public SnappingToolsPreferences CurrentPreferences {
            get => _currentPreferences;
            set => Set(ref _currentPreferences, value);
        }

        public ObservableCollection<SnappingToolsSaveSlot> SaveSlots { get; }


        public SnappingToolsProject() {
            CurrentPreferences = new SnappingToolsPreferences();
            SaveSlots = new ObservableCollection<SnappingToolsSaveSlot>();

            SaveSlots.CollectionChanged += SaveSlotsOnCollectionChanged;
        }

        private void SaveSlotsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            if (e.OldItems != null) {
                foreach (var oldItem in e.OldItems) {
                    ((SnappingToolsSaveSlot) oldItem).ParentProject = null;
                    ((SnappingToolsSaveSlot) oldItem).Dispose();
                }
            }
            if (e.NewItems == null) return;
            foreach (var newItem in e.NewItems) {
                ((SnappingToolsSaveSlot) newItem).ParentProject = this;
            }
        }

        /// <summary>
        /// Set the generators list for this project so they may be automatically updated with preferences.
        /// </summary>
        /// <param name="generators"></param>
        public void SetGenerators(IEnumerable<RelevantObjectsGenerator> generators) {
            _generators = generators;
            if (_generators != null) {
                CurrentPreferences.ApplyGeneratorSettings(_generators);
            }
        }

        public void SetCurrentPreferences(SnappingToolsPreferences preferences) {
            CurrentPreferences = (SnappingToolsPreferences)preferences.Clone();
            if (_generators != null) {
                CurrentPreferences.ApplyGeneratorSettings(_generators);
            }
        }

        public SnappingToolsPreferences GetCurrentPreferences() {
            if (_generators != null) {
                CurrentPreferences.SaveGeneratorSettings(_generators);
            }
            return CurrentPreferences;
        }

        /// <summary>
        /// Use this to get this, so everything is synchronized and good.
        /// </summary>
        /// <returns></returns>
        public SnappingToolsProject GetThis() {
            if (_generators != null) {
                CurrentPreferences.SaveGeneratorSettings(_generators);
            }

            return this;
        }

        public void SaveToSlot(SnappingToolsSaveSlot saveSlot, bool message = true) {
            saveSlot.Preferences = (SnappingToolsPreferences)GetCurrentPreferences().Clone();
            if (message) {
                Task.Factory.StartNew(() => MainWindow.MessageQueue.Enqueue($"Successfully saved settings to {saveSlot.Name}!"));
            }
        }

        public void LoadFromSlot(SnappingToolsSaveSlot saveSlot, bool message = true) {
            SetCurrentPreferences(saveSlot.Preferences);
            if (message) {
                Task.Factory.StartNew(() => MainWindow.MessageQueue.Enqueue($"Successfully loaded settings from {saveSlot.Name}!"));
            }
        }

        public void Activate() {
            foreach (var saveSlot in SaveSlots) {
                saveSlot.Activate();
            }
        }

        public void Deactivate() {
            foreach (var saveSlot in SaveSlots) {
                saveSlot.Deactivate();
            }
        }

        public void Dispose() {
            foreach (var saveSlot in SaveSlots) {
                saveSlot.Dispose();
            }
        }
    }
}