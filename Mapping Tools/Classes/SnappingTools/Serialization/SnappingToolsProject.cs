using System.Collections.Generic;
using System.Collections.ObjectModel;
using Mapping_Tools.Annotations;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators;
using Newtonsoft.Json;

namespace Mapping_Tools.Classes.SnappingTools.Serialization {
    public class SnappingToolsProject {
        [CanBeNull]
        [JsonIgnore]
        private IEnumerable<RelevantObjectsGenerator> _generators;

        public SnappingToolsPreferences CurrentPreferences { get; }

        public ObservableCollection<SnappingToolsPreferences> SaveSlots { get; }


        public SnappingToolsProject() {
            CurrentPreferences = new SnappingToolsPreferences();
            SaveSlots = new ObservableCollection<SnappingToolsPreferences>();
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
            CurrentPreferences.SaveGeneratorSettings(_generators);
            return this;
        }
    }
}