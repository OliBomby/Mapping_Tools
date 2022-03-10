using Mapping_Tools.Components.Graph;

namespace Mapping_Tools.Classes.Tools.TumourGeneratorStuff.Options {
    public interface ITumourLayer {
        ITumourTemplate TumourTemplate { get; }
        int TumourSidedness { get; }
        GraphState TumourLength { get; }
        GraphState TumourScale { get; }
        GraphState TumourRotation { get; }
        GraphState TumourDistance { get; }
        int TumourCount { get; }
        double TumourStart { get; }
        double TumourEnd { get; }
        bool Recalculate { get; }
    }
}