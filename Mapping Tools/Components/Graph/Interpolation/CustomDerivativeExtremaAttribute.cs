using System;

namespace Mapping_Tools.Components.Graph.Interpolation {
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class CustomDerivativeExtremaAttribute : Attribute {
        public double[] ExtremaPositions { get; set; }

        public CustomDerivativeExtremaAttribute(double[] extremaPositions) {
            ExtremaPositions = extremaPositions;
        }
    }
}