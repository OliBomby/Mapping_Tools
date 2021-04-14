using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Mapping_Tools_Core.BeatmapHelper.ComboColours {
    public interface IComboColourCollection {
        /// <summary>
        /// Contains all the basic combo colours.
        /// </summary>
        [NotNull]
        IReadOnlyList<IComboColour> ComboColours { get; }
    }
}