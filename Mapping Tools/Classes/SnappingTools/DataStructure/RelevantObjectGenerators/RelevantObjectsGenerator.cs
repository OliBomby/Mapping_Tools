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

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private MethodInfo _generatorMethod;
        public MethodInfo GetGeneratorMethod() {
            if (_generatorMethod == null) {
                var methods = GetType().GetMethods().Where(m => m.GetCustomAttribute<RelevantObjectGeneratorAttribute>() != null)
                    .ToArray();
                if (methods.Length != 1) {
                    throw new Exception($@"Type {GetType()} does not have exactly one generator method.");
                }

                _generatorMethod = methods[0];
            }

            return _generatorMethod;
        }

        public Type[] GetDependencies() {
            return GetGeneratorMethod().GetGenericArguments();
        }

        public Type GetReturnType() {
            return GetGeneratorMethod().ReturnType;
        }

        private bool? _needsHitObjects;
        public bool NeedsHitObjects() {
            if (_needsHitObjects == null) {
                _needsHitObjects = GetDependencies().Contains(typeof(RelevantHitObject));
            }

            return _needsHitObjects.Value;
        }
    }
}
