using Mapping_Tools.Classes.Tools.TumourGenerator.Enums;
using Mapping_Tools.Components.Graph;

namespace Mapping_Tools.Classes.Tools.TumourGenerator.Options {
    public interface ITumourLayer {
        ITumourTemplate TumourTemplate { get; }
        TumourSidedness TumourSidedness { get; }
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