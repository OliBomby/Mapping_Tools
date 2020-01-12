using System;

namespace Mapping_Tools.Components.Graph.Interpolation {
    public interface IGraphInterpolator {
        double P { get; set; }
        double GetInterpolation(double t);
    }
}