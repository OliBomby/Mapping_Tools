using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Mapping_Tools.Core.Classes.BeatmapHelper;
using Newtonsoft.Json;

namespace Mapping_Tools.Core.Tools.ComboColourStudio;

/// <summary>
/// Associates a beatmap offset with an ordered combo-colour sequence.
/// </summary>
public sealed class ColourPoint : INotifyPropertyChanged, ICloneable
{
    private double _time;
    private ObservableCollection<SpecialColour> _colourSequence;
    private ColourPointMode _mode;
    private bool _isSelected;
    private ComboColourProject? _parentProject;

    /// <summary>Creates an empty normal point at offset zero.</summary>
    public ColourPoint() : this(0, [], ColourPointMode.Normal, null)
    {
    }

    /// <summary>Creates a point with the supplied offset, sequence, and mode.</summary>
    /// <param name="time">The offset in milliseconds.</param>
    /// <param name="colourSequence">The ordered combo-colour references.</param>
    /// <param name="mode">The point application mode.</param>
    /// <param name="parentProject">The owning project, when attached.</param>
    public ColourPoint(
        double time,
        IEnumerable<SpecialColour> colourSequence,
        ColourPointMode mode,
        ComboColourProject? parentProject)
    {
        ArgumentNullException.ThrowIfNull(colourSequence);
        _time = time;
        _colourSequence = new ObservableCollection<SpecialColour>(colourSequence);
        _mode = mode;
        _parentProject = parentProject;
    }

    /// <summary>Raised when editable point state changes.</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Gets or sets the point offset in milliseconds.</summary>
    public double Time
    {
        get => _time;
        set => Set(ref _time, value);
    }

    /// <summary>Gets or sets the ordered colours used by this point.</summary>
    public ObservableCollection<SpecialColour> ColourSequence
    {
        get => _colourSequence;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (ReferenceEquals(_colourSequence, value))
            {
                return;
            }

            _colourSequence = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Gets or sets whether this point is normal or one-combo burst mode.</summary>
    public ColourPointMode Mode
    {
        get => _mode;
        set => Set(ref _mode, value);
    }

    /// <summary>Gets or sets the transient UI selection state.</summary>
    [JsonIgnore]
    public bool IsSelected
    {
        get => _isSelected;
        set => Set(ref _isSelected, value);
    }

    /// <summary>Gets or sets the transient owning project reference.</summary>
    [JsonIgnore]
    public ComboColourProject? ParentProject
    {
        get => _parentProject;
        set => Set(ref _parentProject, value);
    }

    /// <summary>Gets the selectable point modes for a combo-box editor.</summary>
    [JsonIgnore]
    public IReadOnlyList<ColourPointMode> ColourPointModes => Enum.GetValues<ColourPointMode>();

    /// <summary>Creates an independent point copy, including sequence entries.</summary>
    /// <returns>A detached copy with equivalent persisted values.</returns>
    public object Clone() => new ColourPoint(
        Time,
        ColourSequence.Select(colour => (SpecialColour)colour.Clone()),
        Mode,
        ParentProject);

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
