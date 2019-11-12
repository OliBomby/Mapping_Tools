using System.Reflection;
using Mapping_Tools.Annotations;
using Mapping_Tools.Classes.SystemTools;
using Newtonsoft.Json;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators {
    public class GeneratorSettings : BindableBase {
        [JsonIgnore]
        [CanBeNull]
        public RelevantObjectsGenerator Generator { get; set; }

        public GeneratorSettings() {
            _relevancyRatio = 0.9;
        }

        public GeneratorSettings(RelevantObjectsGenerator generator) {
            Generator = generator;
            _relevancyRatio = 0.9;
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

        private double _relevancyRatio;
        public double RelevancyRatio {
            get => _relevancyRatio;
            set => Set(ref _relevancyRatio, value);
        }

        private string _test = "";
        public string Test {
            get => _test;
            set => Set(ref _test, value);
        }

        public void CopyTo(GeneratorSettings other) {
            foreach (var prop in typeof(GeneratorSettings).GetProperties()) {
                if (!prop.CanWrite || !prop.CanRead) continue;
                if (prop.GetCustomAttribute(typeof(JsonIgnoreAttribute)) != null) continue;
                try { prop.SetValue(other, prop.GetValue(this)); } catch { }
            }
        }

        public object Clone() {
            return MemberwiseClone();
        }
    }
}