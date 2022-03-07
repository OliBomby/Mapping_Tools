namespace Mapping_Tools.Classes.Tools.TumourGeneratorStuff {
    public interface ITumourAssignment {
        /// <summary>
        /// The pixel length cumulative distance of the start of the tumour.
        /// </summary>
        double Start { get; }
        
        /// <summary>
        /// The pixel length cumulative distance of the end of the tumour.
        /// </summary>
        double End { get; }

        /// <summary>
        /// The size scalar of tumours.
        /// </summary>
        public double Scalar { get; }

        /// <summary>
        /// The wrapping mode controls how the tumour sits on the slider.
        /// </summary>
        public WrappingMode WrappingMode { get; }

        /// <summary>
        /// Whether to invert the sidedness of the tumour.
        /// If false, up (-Y) will be on the left-hand side of the slider.
        /// If true, up (-Y) will be on the right-hand side of the slider.
        /// </summary>
        public bool Inverted { get; }

        /// <summary>
        /// The tumour shape.
        /// </summary>
        ITumourTemplate GetTemplate();
    }
}