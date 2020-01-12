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

        public static double GetBiggestValue(IGraphInterpolator interpolator) {
            // If the interpolator has a CustomExtremaAttribute than we check the min/max value for all the specified locations
            var customExtremaAttribute =
                interpolator.GetType().GetCustomAttribute<CustomExtremaAttribute>();

            var values = customExtremaAttribute != null
                ? customExtremaAttribute.ExtremaPositions.Select(o => Math.Abs(interpolator.GetInterpolation(o)))
                : new[] {0, 1}.Select(o => Math.Abs(interpolator.GetInterpolation(o)));
                    
            return values.Max();
        }

        public static double GetBiggestDerivative(IGraphInterpolator interpolator) {
            if (!(interpolator is IDerivableInterpolator derivableInterpolator)) return 1;

            // If the interpolator has a CustomDerivativeExtremaAttribute than we check the min/max derivative for all the specified locations
            var customExtremaAttribute =
                derivableInterpolator.GetType().GetCustomAttribute<CustomDerivativeExtremaAttribute>();

            var values = customExtremaAttribute != null
                ? customExtremaAttribute.ExtremaPositions.Select(o => Math.Abs(derivableInterpolator.GetDerivative(o)))
                : new[] {0, 1}.Select(o => Math.Abs(derivableInterpolator.GetDerivative(o)));
                    
            return values.Max();
        }
    }
}