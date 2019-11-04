using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;
using System;
using System.Linq;
using System.Reflection;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators {
    public abstract class RelevantObjectsGenerator
    {
        public GeneratorSettings Settings { get; } = new GeneratorSettings();

        public abstract string Name { get; }
        public abstract string Tooltip { get; }
        public abstract GeneratorType GeneratorType { get; }
        public virtual GeneratorTemporalPositioning TemporalPositioning => GeneratorTemporalPositioning.Average;

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
