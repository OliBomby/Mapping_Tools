using Mapping_Tools.Classes.Tools.TumourGenerating.Enums;
using Mapping_Tools.Components.Graph;

namespace Mapping_Tools.Classes.Tools.TumourGenerating.Options {
    public interface ITumourLayer {
        ITumourTemplate TumourTemplate { get; }

        /// <summary>
        /// The wrapping mode controls how the tumour sits on the slider.
        /// </summary>
        WrappingMode WrappingMode { get; set; }
        TumourSidedness TumourSidedness { get; }
        GraphState TumourLength { get; }
        GraphState TumourScale { get; }
        GraphState TumourRotation { get; }
        GraphState TumourParameter { get; }
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

        /// <summary>
        /// The random seed used for random sidedness.
        /// </summary>
        int RandomSeed { get; }

        /// <summary>
        /// Whether the <see cref="TumourStart"/> and <see cref="TumourEnd"/> are not relative to the slider's length.
        /// </summary>
        bool UseAbsoluteRange { get; }

        bool Recalculate { get; }
        bool IsActive { get; }
    }
}