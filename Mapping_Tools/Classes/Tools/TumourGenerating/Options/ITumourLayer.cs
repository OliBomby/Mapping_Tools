using Mapping_Tools.Classes.Tools.TumourGenerating.Enums;
using Mapping_Tools.Components.Graph;

namespace Mapping_Tools.Classes.Tools.TumourGenerating.Options {
    public interface ITumourLayer {
        ITumourTemplate TumourTemplate { get; }
        TumourSidedness TumourSidedness { get; }
        GraphState TumourLength { get; }
        GraphState TumourScale { get; }
        GraphState TumourRotation { get; }
        GraphState TumourDistance { get; }
        int TumourCount { get; }

        /// <summary>
        /// Completion at which to start generating the tumours.
        /// </summary>
        double TumourStart { get; }

        /// <summary>
        /// Completion at which to stop generating the tumours.
        /// </summary>
        double TumourEnd { get; }
        bool Recalculate { get; }
    }
}