using Mapping_Tools.Annotations;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Mapping_Tools.Classes.SnappingTools.Serialization {
    public class SnappingToolsProject {
        [CanBeNull]
        [JsonIgnore]
        private IEnumerable<RelevantObjectsGenerator> _generators;

        public SnappingToolsPreferences CurrentPreferences { get; }

        public ObservableCollection<SnappingToolsSaveSlot> SaveSlots { get; }


        public SnappingToolsProject() {
            CurrentPreferences = new SnappingToolsPreferences();
            SaveSlots = new ObservableCollection<SnappingToolsSaveSlot>();

            SaveSlots.CollectionChanged += SaveSlotsOnCollectionChanged;
        }

        private void SaveSlotsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
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
            preferences?.CopyTo(CurrentPreferences);
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

        public void SaveToSlot(SnappingToolsSaveSlot snappingToolsSaveSlot) {
            GetCurrentPreferences().CopyTo(snappingToolsSaveSlot.Preferences);
        }

        public void LoadFromSlot(SnappingToolsSaveSlot snappingToolsSaveSlot) {
            SetCurrentPreferences(snappingToolsSaveSlot.Preferences);
        }
    }
}