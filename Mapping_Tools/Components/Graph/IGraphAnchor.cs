using Mapping_Tools.Components.Graph.Interpolation;
using Mapping_Tools.Core.Classes.MathUtil;

namespace Mapping_Tools.Components.Graph {
    public interface IGraphAnchor {
        Vector2 Pos { get; set; }
        IGraphInterpolator Interpolator { get; set; }
        double Tension { get; set; }
    }
}