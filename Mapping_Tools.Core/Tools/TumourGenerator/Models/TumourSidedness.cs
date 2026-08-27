namespace Mapping_Tools.Core.Tools.TumourGenerator.Models;

/// <summary>Chooses the side of the slider on which a tumour is placed.</summary>
public enum TumourSidedness
{
    /// <summary>Places every tumour on the left side.</summary>
    Left,

    /// <summary>Places every tumour on the right side.</summary>
    Right,

    /// <summary>Alternates tumours, beginning on the left side.</summary>
    AlternatingLeft,

    /// <summary>Alternates tumours, beginning on the right side.</summary>
    AlternatingRight,

    /// <summary>Chooses each tumour side from the layer's random sequence.</summary>
    Random,
}

