using Mapping_Tools.Classes.SystemTools;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators {
    public class GeneratorSettings : BindableBase {
        public RelevantObjectsGenerator Generator { get; set; }

        public GeneratorSettings() {

        }

        public GeneratorSettings(RelevantObjectsGenerator generator) {
            Generator = generator;
        }

        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set => Set(ref _isActive, value);
        }

        private bool _isSequential;
        public bool IsSequential {
            get => _isSequential;
            set => Set(ref _isSequential, value);
        }

        private bool _isDeep;
        public bool IsDeep {
            get => _isDeep;
            set => Set(ref _isDeep, value);
        }

        public void CopyTo(GeneratorSettings other) {
            foreach (var prop in typeof(GeneratorSettings).GetProperties()) {
                if (!prop.CanWrite || !prop.CanRead) continue;
                try { prop.SetValue(other, prop.GetValue(this)); } catch { }
            }
        }
    }
}