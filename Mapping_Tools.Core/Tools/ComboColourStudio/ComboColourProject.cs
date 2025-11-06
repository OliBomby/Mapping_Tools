using System.Collections.Generic;
using Mapping_Tools.Core.BeatmapHelper.ComboColours;

namespace Mapping_Tools.Core.Tools.ComboColourStudio;

/// <summary>
/// Implementation of <see cref="IComboColourProject"/>.
/// </summary>
public class ComboColourProject : IComboColourProject {
    private readonly List<IColourPoint> _colourPoints;
    private readonly List<IComboColour> _comboColours;

    ///<inheritdoc/>
    public IReadOnlyList<IColourPoint> ColourPoints => _colourPoints;

    ///<inheritdoc/>
    public IReadOnlyList<IComboColour> ComboColours => _comboColours;

    ///<inheritdoc/>
    public int MaxBurstLength { get; }

    /// <summary>
    /// Creates a new combo colour project.
    /// </summary>
    /// <param name="colourPoints">The colour points.</param>
    /// <param name="comboColours">The combo colours.</param>
    /// <param name="maxBurstLength">The maximum combo length for burst-type colour points.</param>
    public ComboColourProject(IEnumerable<IColourPoint> colourPoints, IEnumerable<IComboColour> comboColours, int maxBurstLength) {
        _colourPoints = new List<IColourPoint>(colourPoints);
        _comboColours = new List<IComboColour>(comboColours);
        MaxBurstLength = maxBurstLength;
    }
}