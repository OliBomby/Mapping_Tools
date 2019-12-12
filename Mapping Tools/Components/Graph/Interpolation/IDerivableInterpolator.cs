namespace Mapping_Tools.Components.Graph.Interpolation {
    public interface IDerivableInterpolator {
        IGraphInterpolator GetDerivativeInterpolator();
        double GetDerivative(double t);
    }
}