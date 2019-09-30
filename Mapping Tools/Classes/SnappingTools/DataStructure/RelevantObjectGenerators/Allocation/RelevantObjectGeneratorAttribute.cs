using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Mapping_Tools.Annotations;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation {
    [MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Method)]
    public class RelevantObjectGeneratorAttribute : Attribute {
        private const BindingFlags ActivatorFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        /// <summary>
        /// Marks this method as the (potentially asynchronous) initializer for a class in the context of dependency injection.
        /// </summary>
        public RelevantObjectGeneratorAttribute() {
        }

        /// <summary>
        /// Gets method with parameters (a, b) that runs the RelevantObjectGenerator method in a with the dependencies from b as the parameters
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static InjectDependencyDelegate CreateActivator(Type type) {
            var loaderMethods = type.GetMethods(ActivatorFlags).Where(m => m.GetCustomAttribute<RelevantObjectGeneratorAttribute>() != null).ToArray();

            switch (loaderMethods.Length) {
                case 0:
                    return (_, __) => { };

                case 1:
                    var method = loaderMethods[0];

                    var parameters = method.GetParameters().Select(p => p.ParameterType).ToArray();

                    if (parameters.Length == 0) {
                        return (target, _) => {
                            try {
                                return method.Invoke(target, new object[0]);
                            } catch (TargetInvocationException exc) // During non-await invocations
                            {
                                ExceptionDispatchInfo.Capture(exc.InnerException).Throw();
                            }
                        };
                    } else {
                        return (target, dc) => {
                            try {
                                List<object> pairs = dc.GetPairsOfType(parameters[0]);
                                return pairs.SelectMany(o => method.Invoke(target, new[] {o}));
                            } catch (TargetInvocationException exc) // During non-await invocations
                            {
                                ExceptionDispatchInfo.Capture(exc.InnerException).Throw();
                            }
                        };
                    }

                    

                default:
                    throw new MultipleDependencyLoaderMethodsException(type);
            }
        }
    }
}