using System;

namespace Mapping_Tools.Components.Graph.Interpolation {
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class CustomExtremaAttribute : Attribute {
        public double[] ExtremaPositions { get; set; }

        public CustomExtremaAttribute(double[] extremaPositions) {
            ExtremaPositions = extremaPositions;
        }
    }
}