using System;
using Mapping_Tools_Core.MathUtil;
using Mapping_Tools_Core.Tools.HitsoundStudio.Model.LayerImportArgs;

namespace Mapping_Tools_Core.Tools.HitsoundStudio.Model.LayerSourceRef {
    public class StackLayerSourceRef : IStackLayerSourceRef {
        public string Path { get; }
        public double X { get; }
        public double Y { get; }
        public double Leniency { get; }

        public StackLayerSourceRef(string path, double x, double y, double leniency) {
            Path = path;
            X = x;
            Y = y;
            Leniency = leniency;
        }

        public bool Equals(ILayerSourceRef other) {
            return other is IStackLayerSourceRef o &&
                   Path == o.Path &&
                   Math.Abs(X - o.X) < Precision.DOUBLE_EPSILON &&
                   Math.Abs(Y - o.Y) < Precision.DOUBLE_EPSILON &&
                   Math.Abs(Leniency - o.Leniency) < Precision.DOUBLE_EPSILON;
        }

        public ILayerImportArgs GetLayerImportArgs() {
            return new StackLayerImportArgs(Path, X, Y, Leniency);
        }

        public bool ReloadCompatible(ILayerSourceRef other) {
            if (!(other is IStackLayerSourceRef o))
                return false;

            if (double.IsNaN(o.X) && !double.IsNaN(X) ||
                double.IsNaN(o.Y) && !double.IsNaN(Y)) {
                // X or Y leniency of other is bigger than this
                return false;
            }

            // Check if the area of the other is a subset of the area of this
            // The leniency is kinda like the radius of the area but for squares
            double maxOffset = Leniency - o.Leniency;
            return Path == o.Path && 
                   (double.IsNaN(X) || Math.Abs(X - o.X) <= maxOffset + Precision.DOUBLE_EPSILON) && 
                   (double.IsNaN(Y) || Math.Abs(Y - o.Y) <= maxOffset + Precision.DOUBLE_EPSILON);
        }
    }
}