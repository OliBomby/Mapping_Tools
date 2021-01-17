using Mapping_Tools_Core.MathUtil;
using System;

namespace Mapping_Tools_Core.Tools.HitsoundStudio.Model.LayerImportArgs {
    public class StackLayerImportArgs : IStackLayerImportArgs {
        public StackLayerImportArgs(string path, double x, double y, double leniency) {
            Path = path;
            X = x;
            Y = y;
            Leniency = leniency;
        }

        public bool Equals(ILayerImportArgs other) {
            return other is IStackLayerImportArgs o &&
                   Path == o.Path &&
                   Math.Abs(X - o.X) < Precision.DOUBLE_EPSILON &&
                   Math.Abs(Y - o.Y) < Precision.DOUBLE_EPSILON &&
                   Math.Abs(Leniency - o.Leniency) < Precision.DOUBLE_EPSILON;
        }

        public string Path { get; }
        public double X { get; }
        public double Y { get; }
        public double Leniency { get; }
    }
}