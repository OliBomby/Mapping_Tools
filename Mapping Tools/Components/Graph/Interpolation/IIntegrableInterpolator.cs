namespace Mapping_Tools.Components.Graph.Interpolation {
    public interface IIntegrableInterpolator {
        IGraphInterpolator GetPrimitiveInterpolator();
        double GetIntegral(double t);
    }
}