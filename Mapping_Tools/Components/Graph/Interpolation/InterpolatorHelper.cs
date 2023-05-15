using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Mapping_Tools.Components.Graph.Interpolation.Interpolators;

namespace Mapping_Tools.Components.Graph.Interpolation {
    public class InterpolatorHelper {
        private static readonly Type InterfaceType = typeof(IGraphInterpolator);

        private static Type[] interpolators;
        public static Type[] GetInterpolators() {
            try {
                return interpolators ?? (interpolators = AppDomain.CurrentDomain.GetAssemblies()
                           .SelectMany(x => x.GetTypes())
                           .Where(x => InterfaceType.IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract &&
                                       x.GetCustomAttribute<IgnoreInterpolatorAttribute>() == null).OrderBy(GetName, new InterpolatorComparer()).ToArray());
            } catch (ReflectionTypeLoadException) {
                return new Type[0];
            }
        }

        /// <summary>
        /// Returns the index of the interpolator type or -1 if it can't be found.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static int GetInterpolatorIndex(Type type) {
            var interpolators = GetInterpolators();
            for (int i = 0; i < interpolators.Length; i++) {
                if (interpolators[i] == type) {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Returns the interpolator type of a certain index or null if it can't be found.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public static Type GetInterpolatorByIndex(int index) {
            var interpolators = GetInterpolators();
            return index < 0 || index >= interpolators.Length ? typeof(SingleCurveInterpolator) : interpolators[index];
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