using System;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Mapping_Tools.Annotations;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators {
    [MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
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

                    var parameterGetters = method.GetParameters().Select(p => p.ParameterType).Select(t => getDependency(t, type, false));

                    return (target, dc) => {
                        try {
                            return method.Invoke(target, parameterGetters.Select(p => p(dc)).ToArray());
                        } catch (TargetInvocationException exc) // During non-await invocations
                        {
                            ExceptionDispatchInfo.Capture(exc.InnerException).Throw();
                        }
                    };

                default:
                    throw new MultipleDependencyLoaderMethodsException(type);
            }
        }

        private static Func<IReadOnlyDependencyContainer, object> getDependency(Type type, Type requestingType, bool permitNulls) => dc => {
            var val = dc.Get(type);
            if (val == null && !permitNulls)
                throw new DependencyNotRegisteredException(requestingType, type);

            return val;
        };
    }
}