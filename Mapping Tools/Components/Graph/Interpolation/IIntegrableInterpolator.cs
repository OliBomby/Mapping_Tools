namespace Mapping_Tools.Components.Graph.Interpolation {
    public interface IIntegrableInterpolator {
        IGraphInterpolator GetPrimitiveInterpolator(double x1, double y1, double x2, double y2);
        double GetIntegral(double t1, double t2);
    }
}