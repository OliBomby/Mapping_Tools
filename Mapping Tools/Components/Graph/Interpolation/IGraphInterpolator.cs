namespace Mapping_Tools.Components.Graph.Interpolation {
    public interface IGraphInterpolator {
        double GetInterpolation(double t, double h1, double h2, double parameter);
    }
}