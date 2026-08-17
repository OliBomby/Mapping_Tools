using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Mapping_Tools.Core.Classes.BeatmapHelper;

namespace Mapping_Tools.Core.Tools.ComboColourStudio;

/// <summary>
/// Serializable, framework-neutral Combo Colour Studio state.
/// </summary>
public sealed class ComboColourProject : INotifyPropertyChanged
{
    private ObservableCollection<ColourPoint> _colourPoints;
    private ObservableCollection<SpecialColour> _comboColours;
    private int _maxBurstLength;

    /// <summary>Creates an empty project with the legacy burst-length default.</summary>
    public ComboColourProject()
    {
        _colourPoints = [];
        _comboColours = [];
        _colourPoints.CollectionChanged += ColourPointsChanged;
        _comboColours.CollectionChanged += ComboColoursChanged;
        MaxBurstLength = 1;
    }

    /// <summary>Gets or sets points in their current editing order.</summary>
    public ObservableCollection<ColourPoint> ColourPoints
    {
        get => _colourPoints;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (ReferenceEquals(_colourPoints, value))
            {
                return;
            }

            _colourPoints.CollectionChanged -= ColourPointsChanged;
            _colourPoints = value;
            _colourPoints.CollectionChanged += ColourPointsChanged;
            AttachPoints();
        }
    }

    /// <summary>Gets or sets the named palette, in editor order.</summary>
    public ObservableCollection<SpecialColour> ComboColours
    {
        get => _comboColours;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (ReferenceEquals(_comboColours, value))
            {
                return;
            }

            _comboColours.CollectionChanged -= ComboColoursChanged;
            _comboColours = value;
            _comboColours.CollectionChanged += ComboColoursChanged;
            MatchComboColourReferences();
        }
    }

    /// <summary>Gets or sets the largest combo eligible for burst points.</summary>
    public int MaxBurstLength
    {
        get => _maxBurstLength;
        set
        {
            if (_maxBurstLength == value)
            {
                return;
            }

            _maxBurstLength = value;
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(nameof(MaxBurstLength)));
        }
    }

    /// <summary>Raised when a project-level editable value changes.</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Adds a point using the supplied values and attaches it to this project.</summary>
    /// <param name="time">The offset in milliseconds.</param>
    /// <param name="colours">The initial ordered sequence.</param>
    /// <param name="mode">The point mode.</param>
    /// <returns>The new attached point.</returns>
    public ColourPoint AddColourPoint(
        double time = 0,
        IEnumerable<SpecialColour>? colours = null,
        ColourPointMode mode = ColourPointMode.Normal)
    {
        ColourPoint point = new(time, colours ?? [], mode, this);
        ColourPoints.Add(point);
        return point;
    }

    /// <summary>Removes selected points, or the last point when none are selected.</summary>
    /// <returns>The number of points removed.</returns>
    public int RemoveSelectedOrLastColourPoints()
    {
        ColourPoint[] selected = ColourPoints.Where(point => point.IsSelected).ToArray();
        if (selected.Length > 0)
        {
            foreach (ColourPoint point in selected)
            {
                ColourPoints.Remove(point);
            }

            return selected.Length;
        }

        if (ColourPoints.Count == 0)
        {
            return 0;
        }

        ColourPoints.RemoveAt(ColourPoints.Count - 1);
        return 1;
    }

    /// <summary>Adds a named palette colour, copying the previous colour when available.</summary>
    /// <returns><see langword="true"/> when a colour was added; otherwise the eight-colour limit was reached.</returns>
    public bool AddComboColour()
    {
        if (ComboColours.Count >= 8)
        {
            return false;
        }

        RgbaColour colour = ComboColours.Count == 0
            ? RgbaColour.White
            : ComboColours[^1].Color;
        ComboColours.Add(new SpecialColour(colour, $"Combo{ComboColours.Count + 1}"));
        return true;
    }

    /// <summary>Removes the last palette colour, preserving sequence entries for later reattachment.</summary>
    /// <returns><see langword="true"/> when a colour was removed.</returns>
    public bool RemoveLastComboColour()
    {
        if (ComboColours.Count == 0)
        {
            return false;
        }

        ComboColours.RemoveAt(ComboColours.Count - 1);
        return true;
    }

    /// <summary>Replaces sequence entries with the matching palette object by name.</summary>
    public void MatchComboColourReferences()
    {
        foreach (ColourPoint point in ColourPoints)
        {
            for (int index = 0; index < point.ColourSequence.Count; index++)
            {
                SpecialColour current = point.ColourSequence[index];
                point.ColourSequence[index] = ComboColours.FirstOrDefault(
                    colour => colour.Name == current.Name) ?? current;
            }
        }
    }

    /// <summary>Creates a deep copy suitable for persistence or background execution.</summary>
    /// <returns>An independently mutable project copy.</returns>
    public ComboColourProject Copy()
    {
        ComboColourProject copy = new() { MaxBurstLength = MaxBurstLength };
        foreach (SpecialColour colour in ComboColours)
        {
            copy.ComboColours.Add((SpecialColour)colour.Clone());
        }

        foreach (ColourPoint point in ColourPoints)
        {
            copy.ColourPoints.Add((ColourPoint)point.Clone());
        }

        copy.MatchComboColourReferences();
        return copy;
    }

    /// <summary>Validates the state before it is applied to a beatmap.</summary>
    /// <returns>Human-readable validation failures, in deterministic order.</returns>
    public IReadOnlyList<string> ValidateForExport()
    {
        List<string> errors = [];
        if (MaxBurstLength < 0)
        {
            errors.Add("Max burst length cannot be negative.");
        }

        if (ComboColours.Count == 0)
        {
            errors.Add("Add at least one combo colour before running the tool.");
        }

        string?[] names = ComboColours.Select(colour => colour.Name).ToArray();
        if (names.Any(string.IsNullOrWhiteSpace))
        {
            errors.Add("Every combo colour must have a name.");
        }

        if (names.Where(name => !string.IsNullOrWhiteSpace(name))
            .GroupBy(name => name, StringComparer.Ordinal)
            .Any(group => group.Count() > 1))
        {
            errors.Add("Combo colour names must be unique.");
        }

        HashSet<string?> nameSet = new(names, StringComparer.Ordinal);
        foreach (ColourPoint point in ColourPoints)
        {
            if (!double.IsFinite(point.Time))
            {
                errors.Add("Every colour point offset must be a finite number.");
            }

            foreach (SpecialColour colour in point.ColourSequence)
            {
                if (!nameSet.Contains(colour.Name))
                {
                    errors.Add($"Colour point at offset {point.Time} references missing colour '{colour.Name}'.");
                }
            }
        }

        return errors;
    }

    private void ColourPointsChanged(object? sender, NotifyCollectionChangedEventArgs eventArgs)
    {
        if (eventArgs.OldItems is not null)
        {
            foreach (ColourPoint point in eventArgs.OldItems)
            {
                point.ParentProject = null;
            }
        }

        if (eventArgs.NewItems is not null)
        {
            foreach (ColourPoint point in eventArgs.NewItems)
            {
                point.ParentProject = this;
            }
        }

        MatchComboColourReferences();
    }

    private void ComboColoursChanged(object? sender, NotifyCollectionChangedEventArgs eventArgs) =>
        MatchComboColourReferences();

    private void AttachPoints()
    {
        foreach (ColourPoint point in ColourPoints)
        {
            point.ParentProject = this;
        }

        MatchComboColourReferences();
    }
}
