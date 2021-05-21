using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Components.Graph.Interpolation;

namespace Mapping_Tools.Components.Graph {
    public interface IGraphAnchor {
        Vector2 Pos { get; set; }
        IGraphInterpolator Interpolator { get; set; }
        double Tension { get; set; }
    }
}