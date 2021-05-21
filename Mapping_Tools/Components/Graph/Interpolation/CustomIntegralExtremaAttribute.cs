using System;

namespace Mapping_Tools.Components.Graph.Interpolation {
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class CustomIntegralExtremaAttribute : Attribute {
        public double[] ExtremaPositions { get; set; }

        public CustomIntegralExtremaAttribute(double[] extremaPositions) {
            ExtremaPositions = extremaPositions;
        }
    }
}