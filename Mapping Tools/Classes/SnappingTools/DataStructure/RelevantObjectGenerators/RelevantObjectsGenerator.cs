using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Mapping_Tools.Annotations;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators {
    public abstract class RelevantObjectsGenerator : INotifyPropertyChanged
    {
        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set {
                if (value == _isActive) return;
                _isActive = value;
                OnPropertyChanged();
            }
        }

        private bool _isConcurrent;
        public bool IsConcurrent {
            get => _isConcurrent;
            set {
                if (value == _isConcurrent) return;
                _isConcurrent = value;
                OnPropertyChanged();
            }
        }

        public abstract string Name { get; }
        public abstract string Tooltip { get; }
        public abstract GeneratorType GeneratorType { get; }
        public virtual GeneratorTemporalPositioning TemporalPositioning => GeneratorTemporalPositioning.Average;

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private MethodInfo[] _generatorMethods;
        public MethodInfo[] GetGeneratorMethods() {
            if (_generatorMethods != null) return _generatorMethods;
            var methods = GetType().GetMethods().Where(m => m.GetCustomAttribute<RelevantObjectsGeneratorMethodAttribute>() != null)
                .ToArray();
            if (methods.Length == 0) {
                throw new InvalidOperationException($@"Type {GetType()} does not have any generator method.");
            }

            _generatorMethods = methods;

            return _generatorMethods;
        }

        public static Type[] GetDependencies(MethodInfo generatorMethodInfo) {
            return generatorMethodInfo.GetParameters().Select(o => o.ParameterType).ToArray();
        }

        public static Type GetReturnType(MethodInfo generatorMethodInfo) {
            return generatorMethodInfo.ReturnType;
        }

        private static bool? _needsHitObjects;
        public static bool NeedsHitObjects(MethodInfo generatorMethodInfo) {
            if (_needsHitObjects == null) {
                _needsHitObjects = GetDependencies(generatorMethodInfo).Contains(typeof(RelevantHitObject));
            }

            return _needsHitObjects.Value;
        }
    }
}
