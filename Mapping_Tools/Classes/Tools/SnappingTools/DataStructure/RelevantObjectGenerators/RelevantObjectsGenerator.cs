using System;
using System.Linq;
using System.Reflection;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;
using Mapping_Tools.Components.Domain;
using Mapping_Tools.Views.SnappingTools;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators {
    public abstract class RelevantObjectsGenerator
    {
        public GeneratorSettings Settings { get; }
        
        public CommandImplementation GeneratorSettingsCommand { get; }

        public abstract string Name { get; }
        public abstract string Tooltip { get; }
        public abstract GeneratorType GeneratorType { get; }
        public virtual GeneratorTemporalPositioning TemporalPositioning => GeneratorTemporalPositioning.Average;

        protected RelevantObjectsGenerator() {
            Settings = new GeneratorSettings(this);

            // Make command
            GeneratorSettingsCommand = new CommandImplementation(
                e => {
                    try {
                        var settingsWindow = new GeneratorSettingsWindow(Settings);
                        settingsWindow.ShowDialog();
                    } catch (Exception ex) { ex.Show(); }
                });
        }

        protected RelevantObjectsGenerator(GeneratorSettings settings) {
            Settings = settings;

            // Make command
            GeneratorSettingsCommand = new CommandImplementation(
                e => {
                    try {
                        var settingsWindow = new GeneratorSettingsWindow(Settings);
                        settingsWindow.ShowDialog();
                    } catch (Exception ex) { ex.Show(); }
                });
        }

        private MethodInfo[] _generatorMethods;
        public MethodInfo[] GetGeneratorMethods() {
            if (_generatorMethods != null) return _generatorMethods;
            var methods = GetType().GetMethods().Where(m => CustomAttributeExtensions.GetCustomAttribute<RelevantObjectsGeneratorMethodAttribute>((MemberInfo) m) != null)
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
    }
}
