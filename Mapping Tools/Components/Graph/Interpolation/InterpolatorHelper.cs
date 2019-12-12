using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace Mapping_Tools.Components.Graph.Interpolation {
    public class InterpolatorHelper {
        private static readonly Type InterfaceType = typeof(IGraphInterpolator);

        private static Type[] _interpolators;
        public static Type[] GetInterpolators() {
            try {
                return _interpolators ?? (_interpolators = AppDomain.CurrentDomain.GetAssemblies()
                           .SelectMany(x => x.GetTypes())
                           .Where(x => InterfaceType.IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract &&
                                       x.GetCustomAttribute<IgnoreInterpolatorAttribute>() == null).OrderBy(GetName, new InterpolatorComparer()).ToArray());
            } catch (ReflectionTypeLoadException) {
                return new Type[0];
            }
        }

        public static string GetName(Type type) {
            var nameAttribute = type.GetCustomAttribute<DisplayNameAttribute>();
            return nameAttribute != null ? nameAttribute.DisplayName : type.Name;
        }

        public static IGraphInterpolator GetInterpolator(Type type) {
            return (IGraphInterpolator) Activator.CreateInstance(type);
        }
    }
}